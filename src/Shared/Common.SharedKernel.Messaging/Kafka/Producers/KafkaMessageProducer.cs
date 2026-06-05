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
        MessageEnvelope<T> envelope = MessageEnvelope<T>.Create(resolvedTopic, message, metadata);
        byte[] payload = serializer.Serialize(envelope);
        Message<string, byte[]> kafkaMessage = new()
        {
            Key = metadata.Key ?? envelope.MessageId,
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
                DeliveryResult<string, byte[]> report = metadata.Partition.HasValue
                    ? await _producer.ProduceAsync(new TopicPartition(resolvedTopic, new Partition(metadata.Partition.Value)), kafkaMessage, cancellationToken)
                    : await _producer.ProduceAsync(resolvedTopic, kafkaMessage, cancellationToken);

                stopwatch.Stop();
                instrumentation.PublishDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds);
                await logger.LogInformationAsync("Message published", "messaging.publish", new Dictionary<string, object?>
                {
                    ["messageId"] = envelope.MessageId,
                    ["topic"] = resolvedTopic,
                    ["provider"] = "Kafka",
                    ["durationMs"] = stopwatch.Elapsed.TotalMilliseconds,
                    ["partition"] = report.Partition.Value,
                    ["offset"] = report.Offset.Value
                }, cancellationToken);
                return;
            }
            catch (Exception) when (attempt < attempts)
            {
                instrumentation.RetryCount.Add(1);
                await logger.LogWarningAsync("Message publish retry", "messaging.retry", new Dictionary<string, object?>
                {
                    ["messageId"] = envelope.MessageId,
                    ["topic"] = resolvedTopic,
                    ["provider"] = "Kafka",
                    ["attempt"] = attempt
                }, cancellationToken);

                await Task.Delay(GetDelay(attempt), cancellationToken);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                instrumentation.PublishFailures.Add(1);
                await logger.LogErrorAsync("Message publish failed", "messaging.publish", ex, new Dictionary<string, object?>
                {
                    ["messageId"] = envelope.MessageId,
                    ["topic"] = resolvedTopic,
                    ["provider"] = "Kafka",
                    ["durationMs"] = stopwatch.Elapsed.TotalMilliseconds
                }, cancellationToken);
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

    private TimeSpan GetDelay(int attempt)
    {
        double multiplier = Math.Pow(_options.RetryPolicy.BackoffMultiplier, attempt - 1);
        return TimeSpan.FromMilliseconds(_options.RetryPolicy.InitialDelay.TotalMilliseconds * multiplier);
    }

    public void Dispose() => _producer.Dispose();
}
