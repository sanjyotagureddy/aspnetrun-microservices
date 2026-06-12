using System.Diagnostics;
using System.Text;
using Common.SharedKernel.Logging;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Common.SharedKernel.Messaging;

internal sealed class KafkaMessageProducer(
    IOptions<MessagingOptions> options,
    IMessageSerializer serializer,
    ILogger<KafkaMessageProducer> logger,
    MessagingInstrumentation instrumentation) : IMessageProducer, IDisposable
{
    private readonly MessagingOptions _options = options.Value;
    private readonly IProducer<string, byte[]> _producer = new ProducerBuilder<string, byte[]>(new ProducerConfig
    {
        BootstrapServers = options.Value.Kafka.BootstrapServers,
        Acks = Acks.All,
        EnableIdempotence = true
    }).Build();

    public Task PublishAsync<T>(string topic, T message, MessageMetadata metadata, CancellationToken cancellationToken = default)
        => PublishCoreAsync(topic, message, metadata, cancellationToken);

    public async Task PublishBatchAsync<T>(
        string topic,
        IReadOnlyCollection<T> messages,
        MessageMetadata metadata,
        CancellationToken cancellationToken = default)
    {
        foreach (T message in messages)
        {
            MessageMetadata itemMetadata = metadata.Clone();
            itemMetadata.MessageId = Guid.NewGuid().ToString("N");
            await PublishCoreAsync(topic, message, itemMetadata, cancellationToken);
        }
    }

    private async Task PublishCoreAsync<T>(string topic, T message, MessageMetadata metadata, CancellationToken cancellationToken)
    {
        string resolvedTopic = ResolveTopic(topic);
        DestinationRegistration? registration = ResolveRegistration(topic, resolvedTopic);
        int? explicitPartition = ResolveExplicitPartition(metadata);
        ValidatePartitionPolicy(metadata, registration, resolvedTopic, explicitPartition);

        MessageEnvelope<T> envelope = MessageEnvelope<T>.Create(resolvedTopic, message, metadata);
        string eventType = ResolveEventType(metadata, envelope);
        string messageKey = ResolveMessageKey(envelope, metadata);
        byte[] payload = serializer.Serialize(envelope);
        Message<string, byte[]> kafkaMessage = new()
        {
            Key = messageKey,
            Value = payload,
            Headers = BuildHeaders(envelope, metadata)
        };

        using Activity? activity = instrumentation.ActivitySource.StartActivity("messaging.publish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "kafka");
        activity?.SetTag("messaging.destination.name", resolvedTopic);
        activity?.SetTag("messaging.message.id", envelope.MessageId);

        Stopwatch stopwatch = Stopwatch.StartNew();
        int attempts = Math.Max(1, _options.RetryPolicy.MaxAttempts);

        for (int attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                if (explicitPartition.HasValue)
                {
                    await _producer.ProduceAsync(new TopicPartition(resolvedTopic, new Partition(explicitPartition.Value)), kafkaMessage, cancellationToken);
                }
                else
                {
                    await _producer.ProduceAsync(resolvedTopic, kafkaMessage, cancellationToken);
                }

                stopwatch.Stop();
                instrumentation.PublishDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds);
                return;
            }
            catch (Exception) when (attempt < attempts)
            {
                instrumentation.RetryCount.Add(1);
                await Task.Delay(GetDelay(attempt), cancellationToken);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                instrumentation.PublishFailures.Add(1);
                await logger.LogEventAsync(
                    new ErrorLog
                    {
                        Message = "Message publish failed",
                        Category = "messaging.publish",
                        Exception = ex,
                        ExceptionType = ex.GetType().FullName,
                        ExceptionMessage = ex.Message,
                        Context = new Dictionary<string, object?>
                        {
                            ["messageId"] = envelope.MessageId,
                            ["eventType"] = eventType,
                            ["contractVersion"] = envelope.Contract.Version,
                            ["contractCompatibility"] = envelope.Contract.Compatibility.ToString(),
                            ["topic"] = resolvedTopic,
                            ["provider"] = "Kafka",
                            ["durationMs"] = stopwatch.Elapsed.TotalMilliseconds
                        }
                    },
                    cancellationToken);
                throw new MessagingException($"Failed to publish message to topic '{resolvedTopic}'.", ex);
            }
        }
    }

    private Headers BuildHeaders<T>(IMessageEnvelope<T> envelope, MessageMetadata metadata)
    {
        Headers headers = [];
        Add(headers, KafkaMessageHeaderNames.MessageId, envelope.MessageId);
        Add(headers, KafkaMessageHeaderNames.CorrelationId, envelope.CorrelationId);
        Add(headers, KafkaMessageHeaderNames.CausationId, envelope.CausationId);
        Add(headers, KafkaMessageHeaderNames.TraceId, metadata.TraceId);
        Add(headers, KafkaMessageHeaderNames.SpanId, metadata.SpanId);
        Add(headers, KafkaMessageHeaderNames.TenantId, envelope.TenantId);
        Add(headers, KafkaMessageHeaderNames.ContentType, serializer.ContentType);
        Add(headers, KafkaMessageHeaderNames.ContractType, envelope.Contract.MessageType);
        Add(headers, KafkaMessageHeaderNames.ContractVersion, envelope.Contract.Version);
        Add(headers, KafkaMessageHeaderNames.ContractCompatibility, envelope.Contract.Compatibility.ToString());

        foreach (KeyValuePair<string, string> header in envelope.Headers)
        {
            Add(headers, header.Key, header.Value);
        }

        return headers;
    }

    private static void Add(Headers headers, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            headers.Add(key, Encoding.UTF8.GetBytes(value));
        }
    }

    private string ResolveTopic(string topic)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        return string.IsNullOrWhiteSpace(_options.TopicPrefix) ? topic : $"{_options.TopicPrefix}.{topic}";
    }

    private string ResolveMessageKey<T>(IMessageEnvelope<T> envelope, MessageMetadata metadata)
    {
        if (!string.IsNullOrWhiteSpace(metadata.OrderingKey))
        {
            return metadata.OrderingKey;
        }

        if (!string.IsNullOrWhiteSpace(metadata.RoutingKey))
        {
            return metadata.RoutingKey;
        }

        return envelope.MessageId;
    }

    private static string ResolveEventType<T>(MessageMetadata metadata, IMessageEnvelope<T> envelope)
    {
        if (metadata.Headers.TryGetValue("EventType", out string? eventTypeFromHeader)
            && !string.IsNullOrWhiteSpace(eventTypeFromHeader))
        {
            return eventTypeFromHeader;
        }

        return envelope.Contract.MessageType;
    }

    private DestinationRegistration? ResolveRegistration(string topic, string resolvedTopic)
        => _options.Destinations.FirstOrDefault(destination =>
            string.Equals(destination.DestinationName, topic, StringComparison.OrdinalIgnoreCase)
            || string.Equals(destination.DestinationName, resolvedTopic, StringComparison.OrdinalIgnoreCase));

    private static void ValidatePartitionPolicy(MessageMetadata metadata, DestinationRegistration? registration, string topic, int? explicitPartition)
    {
        if (registration is null)
        {
            return;
        }

        string? effectiveKey = !string.IsNullOrWhiteSpace(metadata.OrderingKey)
            ? metadata.OrderingKey
            : metadata.RoutingKey;

        if (registration.OrderingRequired && string.IsNullOrWhiteSpace(effectiveKey) && registration.PartitioningStrategy != PartitioningStrategy.ExplicitPartition)
        {
            throw new MessagingConfigurationException($"Destination '{topic}' requires an OrderingKey or RoutingKey.");
        }

        if (registration.PartitioningStrategy != PartitioningStrategy.ExplicitPartition && explicitPartition.HasValue)
        {
            throw new MessagingConfigurationException($"Destination '{topic}' does not allow explicit partition assignment.");
        }

        if (!string.IsNullOrWhiteSpace(effectiveKey) && string.Equals(effectiveKey, metadata.MessageId, StringComparison.OrdinalIgnoreCase))
        {
            throw new MessagingConfigurationException($"Destination '{topic}' cannot use message/event id as partition key.");
        }
    }

    private static int? ResolveExplicitPartition(MessageMetadata metadata)
    {
        return metadata.TryGetKafkaPartition(out int partitionFromHint)
            ? partitionFromHint
            : null;
    }

    private TimeSpan GetDelay(int attempt)
    {
        double multiplier = Math.Pow(_options.RetryPolicy.BackoffMultiplier, attempt - 1);
        return TimeSpan.FromMilliseconds(_options.RetryPolicy.InitialDelay.TotalMilliseconds * multiplier);
    }

    public void Dispose() => _producer.Dispose();
}
