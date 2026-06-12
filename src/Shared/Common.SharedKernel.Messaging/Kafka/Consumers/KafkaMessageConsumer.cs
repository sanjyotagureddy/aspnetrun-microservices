using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Common.SharedKernel.Logging;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Common.SharedKernel.Messaging;

internal sealed class KafkaMessageConsumer(
    IOptions<MessagingOptions> options,
    IMessageSerializer serializer,
    IEnumerable<IMessageUpcaster> upcasters,
    ILogger<KafkaMessageConsumer> logger,
    MessagingInstrumentation instrumentation) : IMessageConsumer, IDisposable
{
    private readonly MessagingOptions _options = options.Value;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _subscriptions = new(StringComparer.OrdinalIgnoreCase);

    public Task SubscribeAsync<T>(string topic, IMessageHandler<T> handler, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(handler);

        var resolvedTopic = ResolveTopic(topic);
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        if (!_subscriptions.TryAdd(resolvedTopic, linkedCts))
        {
            throw new MessagingException($"A consumer is already subscribed to topic '{resolvedTopic}'.");
        }

        _ = Task.Run(() => ConsumeLoopAsync(resolvedTopic, handler, linkedCts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    public Task UnsubscribeAsync(string topic, CancellationToken cancellationToken = default)
    {
        var resolvedTopic = ResolveTopic(topic);
        if (!_subscriptions.TryRemove(resolvedTopic, out CancellationTokenSource? cts)) return Task.CompletedTask;
        cts.Cancel();
        cts.Dispose();

        return Task.CompletedTask;
    }

    private async Task ConsumeLoopAsync<T>(string topic, IMessageHandler<T> handler, CancellationToken cancellationToken)
    {
        ConsumerConfig config = new()
        {
            BootstrapServers = _options.Kafka.BootstrapServers,
            GroupId = _options.Kafka.ConsumerGroup,
            EnableAutoCommit = _options.Kafka.EnableAutoCommit,
            AutoOffsetReset = Enum.TryParse(_options.Kafka.AutoOffsetReset, true, out AutoOffsetReset reset) ? reset : AutoOffsetReset.Earliest
        };

        using IConsumer<string, byte[]> consumer = new ConsumerBuilder<string, byte[]>(config).Build();
        consumer.Subscribe(topic);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ConsumeResult<string, byte[]>? result = consumer.Consume(_options.Kafka.ConsumeTimeout);
                if (result is null)
                {
                    continue;
                }

                await HandleResultAsync(topic, handler, consumer, result, cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            consumer.Close();
        }
    }

    private async Task HandleResultAsync<T>(
        string topic,
        IMessageHandler<T> handler,
        IConsumer<string, byte[]> consumer,
        ConsumeResult<string, byte[]> result,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        Dictionary<string, string> headers = ReadHeaders(result.Message.Headers);
        IMessageEnvelope<T> envelope;

        try
        {
            envelope = serializer.Deserialize<T>(result.Message.Value);

            if (TryResolveExpectedContract(handler, topic, out MessageContractDescriptor expectedContract)
                && !MessageContractCompatibility.IsCompatible(
                    envelope.Contract,
                    expectedContract,
                    (handler as IContractAwareMessageHandler)?.SupportedContractVersions,
                    (handler as IContractAwareMessageHandler)?.SupportedMessageType))
            {
                IMessageEnvelope<T>? upcastedEnvelope = TryUpcast(envelope, expectedContract, upcasters);
                if (upcastedEnvelope is null)
                {
                    throw new MessagingConfigurationException(
                        $"Message contract '{envelope.Contract.MessageType}:{envelope.Contract.Version}' is not compatible with expected '{expectedContract.MessageType}:{expectedContract.Version}' for topic '{topic}'.");
                }

                envelope = upcastedEnvelope;
            }
        }
        catch (Exception ex)
        {
            instrumentation.ConsumeFailures.Add(1);
            await LogDeadLetterAsync(headers.GetValueOrDefault(KafkaMessageHeaderNames.MessageId) ?? string.Empty, topic, "DeserializationOrCompatibilityFailure", ex, cancellationToken);
            return;
        }

        MessageContext context = new(
            envelope.MessageId,
            envelope.CorrelationId,
            headers.GetValueOrDefault(KafkaMessageHeaderNames.TraceId),
            headers.GetValueOrDefault(KafkaMessageHeaderNames.SpanId),
            envelope.TenantId,
            topic,
            result.Partition.Value,
            result.Offset.Value,
            headers);

        int attempts = Math.Max(1, _options.RetryPolicy.MaxAttempts);
        for (int attempt = 1; attempt <= attempts; attempt++)
        {
            try
            {
                using Activity? activity = instrumentation.ActivitySource.StartActivity("messaging.consume", ActivityKind.Consumer);
                activity?.SetTag("messaging.system", "kafka");
                activity?.SetTag("messaging.destination.name", topic);
                activity?.SetTag("messaging.message.id", envelope.MessageId);

                await handler.HandleAsync(envelope.Payload, context, cancellationToken);
                if (!_options.Kafka.EnableAutoCommit)
                {
                    consumer.Commit(result);
                }

                stopwatch.Stop();
                instrumentation.ConsumeDurationMs.Record(stopwatch.Elapsed.TotalMilliseconds);
                await logger.LogTraceAsync(
                    new TraceLog
                    {
                        Message = "Message consumed",
                        Category = "messaging.consume",
                        DurationMs = stopwatch.Elapsed.TotalMilliseconds,
                        Context = new Dictionary<string, object?>
                        {
                            ["messageId"] = envelope.MessageId,
                            ["consumerGroup"] = _options.Kafka.ConsumerGroup,
                            ["handler"] = handler.GetType().Name,
                            ["topic"] = topic
                        }
                    },
                    LogType.Event,
                    cancellationToken);
                return;
            }
            catch (Exception) when (attempt < attempts)
            {
                instrumentation.RetryCount.Add(1);
                await logger.LogTraceAsync(
                    new TraceLog
                    {
                        Message = "Message consume retry",
                        Category = "messaging.retry",
                        Context = new Dictionary<string, object?>
                        {
                            ["messageId"] = envelope.MessageId,
                            ["consumerGroup"] = _options.Kafka.ConsumerGroup,
                            ["attempt"] = attempt
                        }
                    },
                    LogType.Event,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                instrumentation.ConsumeFailures.Add(1);
                await LogDeadLetterAsync(envelope.MessageId, topic, "HandlerFailure", ex, cancellationToken);
            }
        }
    }

    private async Task LogDeadLetterAsync(string messageId, string topic, string reason, Exception exception, CancellationToken cancellationToken)
    {
        instrumentation.DeadLetterCount.Add(1);
        await logger.LogErrorAsync(
            new ErrorLog
            {
                Message = "Message moved to dead letter",
                Category = "messaging.deadletter",
                Exception = exception,
                ExceptionType = exception.GetType().FullName,
                ExceptionMessage = exception.Message,
                Context = new Dictionary<string, object?>
                {
                    ["messageId"] = messageId,
                    ["topic"] = topic,
                    ["reason"] = reason,
                    ["provider"] = "Kafka"
                }
            },
            LogType.Event,
            cancellationToken);
    }

    private static Dictionary<string, string> ReadHeaders(Headers? headers)
    {
        Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
        if (headers is null)
        {
            return values;
        }

        foreach (IHeader header in headers)
        {
            values[header.Key] = Encoding.UTF8.GetString(header.GetValueBytes());
        }

        return values;
    }

    private string ResolveTopic(string topic)
        => string.IsNullOrWhiteSpace(_options.TopicPrefix) ? topic : $"{_options.TopicPrefix}.{topic}";

    private bool TryResolveExpectedContract<T>(IMessageHandler<T> handler, string topic, out MessageContractDescriptor expectedContract)
    {
        if (handler is IContractAwareMessageHandler contractAware
            && !string.IsNullOrWhiteSpace(contractAware.SupportedMessageType)
            && contractAware.SupportedContractVersions.Count > 0)
        {
            string version = contractAware.SupportedContractVersions.OrderByDescending(value => value, StringComparer.OrdinalIgnoreCase).First();
            expectedContract = new MessageContractDescriptor(contractAware.SupportedMessageType, version, "application/json", Compatibility: CompatibilityMode.Full);
            return true;
        }

        DestinationRegistration? registration = _options.Destinations.FirstOrDefault(destination =>
            string.Equals(destination.DestinationName, topic, StringComparison.OrdinalIgnoreCase)
            || string.Equals(ResolveTopic(destination.DestinationName), topic, StringComparison.OrdinalIgnoreCase));

        if (registration is null)
        {
            expectedContract = MessageContractDescriptor.Unspecified;
            return false;
        }

        expectedContract = registration.Contract;
        return true;
    }

    private static IMessageEnvelope<T>? TryUpcast<T>(
        IMessageEnvelope<T> envelope,
        MessageContractDescriptor expectedContract,
        IEnumerable<IMessageUpcaster> upcasters)
    {
        foreach (IMessageUpcaster upcaster in upcasters)
        {
            if (!upcaster.CanUpcast(envelope.Contract, expectedContract, typeof(T)))
            {
                continue;
            }

            IMessageEnvelope<T> upcasted = upcaster.Upcast(envelope, expectedContract);
            return MessageContractCompatibility.IsCompatible(upcasted.Contract, expectedContract)
                ? upcasted
                : null;
        }

        return null;
    }

    public void Dispose()
    {
        foreach (CancellationTokenSource cts in _subscriptions.Values)
        {
            cts.Cancel();
            cts.Dispose();
        }
    }
}
