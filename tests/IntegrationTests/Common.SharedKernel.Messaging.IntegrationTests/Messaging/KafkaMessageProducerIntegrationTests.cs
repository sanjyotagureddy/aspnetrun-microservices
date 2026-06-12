using Common.SharedKernel.Messaging.IntegrationTests.Fixtures;
using Common.SharedKernel.Messaging.IntegrationTests.Support;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Options;

namespace Common.SharedKernel.Messaging.IntegrationTests.Messaging;

public sealed class KafkaMessageProducerIntegrationTests(KafkaFixture fixture) : IClassFixture<KafkaFixture>
{
    private readonly KafkaFixture _fixture = fixture;

    [Fact]
    public async Task PublishAsync_ShouldWriteEnvelopeAndHeadersToKafka()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        string topic = $"messaging.integration.{Guid.NewGuid():N}";

        await EnsureTopicExistsAsync(topic, cancellationToken);

        MessagingOptions options = new()
        {
            Provider = MessagingProviderKind.Kafka,
            Kafka = new KafkaMessagingOptions { BootstrapServers = _fixture.BootstrapServers },
            RetryPolicy = new RetryPolicyOptions { MaxAttempts = 1 }
        };

        var logger = new RecordingLogger<KafkaMessageProducer>();
        using var instrumentation = new MessagingInstrumentation();
        using var producer = new KafkaMessageProducer(
            Options.Create(options),
            new SystemTextJsonMessageSerializer(),
            logger,
            instrumentation);

        MessageMetadata metadata = new()
        {
            CorrelationId = "corr-456",
            OrderingKey = "order-1"
        };
        metadata.Headers["EventType"] = "product.created";

        ProductCreatedMessage payload = new(Guid.NewGuid(), "SKU-100");

        await producer.PublishAsync(topic, payload, metadata, cancellationToken);

        using var consumer = new ConsumerBuilder<string, byte[]>(new ConsumerConfig
        {
            BootstrapServers = _fixture.BootstrapServers,
            GroupId = $"messaging-integration-{Guid.NewGuid():N}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        }).Build();

        consumer.Subscribe(topic);
        ConsumeResult<string, byte[]> consumed = await ConsumeAsync(consumer, cancellationToken);

        consumed.Message.Key.Should().Be("order-1");

        var serializer = new SystemTextJsonMessageSerializer();
        IMessageEnvelope<ProductCreatedMessage> envelope = serializer.Deserialize<ProductCreatedMessage>(consumed.Message.Value);

        envelope.Payload.ProductId.Should().Be(payload.ProductId);
        envelope.Payload.Sku.Should().Be(payload.Sku);
        envelope.CorrelationId.Should().Be("corr-456");

        consumed.Message.Headers.Should().NotBeNull();
        consumed.Message.Headers.GetLastBytes(KafkaMessageHeaderNames.ContractType)
            .Should().NotBeNull();

        logger.ErrorEntries.Should().BeEmpty();
    }

    private async Task EnsureTopicExistsAsync(string topic, CancellationToken cancellationToken)
    {
        using var admin = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = _fixture.BootstrapServers
        }).Build();

        try
        {
            await admin.CreateTopicsAsync(
            new[]
            {
                new TopicSpecification
                {
                    Name = topic,
                    NumPartitions = 1,
                    ReplicationFactor = 1
                }
            });
        }
        catch (CreateTopicsException ex) when (ex.Results.All(result => result.Error.Code == ErrorCode.TopicAlreadyExists))
        {
            // No-op if topic already exists.
        }

        await Task.Delay(TimeSpan.FromMilliseconds(300), cancellationToken);
    }

    private static async Task<ConsumeResult<string, byte[]>> ConsumeAsync(IConsumer<string, byte[]> consumer, CancellationToken cancellationToken)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(15);

        while (DateTime.UtcNow < deadline)
        {
            ConsumeResult<string, byte[]>? result = consumer.Consume(TimeSpan.FromMilliseconds(500));
            if (result is not null)
            {
                return result;
            }

            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();
        }

        throw new TimeoutException("Expected Kafka message was not consumed within 15 seconds.");
    }

    private sealed record ProductCreatedMessage(Guid ProductId, string Sku);
}