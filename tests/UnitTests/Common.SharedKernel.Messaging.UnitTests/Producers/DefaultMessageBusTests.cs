using NSubstitute;

namespace Common.SharedKernel.Messaging.UnitTests.Producers;

public sealed class DefaultMessageBusTests
{
    [Fact]
    public async Task PublishAsync_ShouldCallProducerOnce_WithConfiguredMetadata()
    {
        IMessageProducer producer = Substitute.For<IMessageProducer>();
        DefaultMessageBus bus = new(producer);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await bus.PublishAsync("products.events.v1", new TestPayload("P-1"), metadata =>
        {
            metadata.CorrelationId = "corr-123";
            metadata.OrderingKey = "product-1";
        }, cancellationToken);

        await producer.Received(1).PublishAsync(
            "products.events.v1",
            Arg.Any<TestPayload>(),
            Arg.Is<MessageMetadata>(metadata =>
                metadata.CorrelationId == "corr-123"
                && metadata.OrderingKey == "product-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PublishBatchAsync_ShouldCallProducerBatchOnce()
    {
        IMessageProducer producer = Substitute.For<IMessageProducer>();
        DefaultMessageBus bus = new(producer);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IReadOnlyCollection<TestPayload> batch =
        [
            new TestPayload("P-1"),
            new TestPayload("P-2")
        ];

        await bus.PublishBatchAsync("products.events.v1", batch, metadata =>
        {
            metadata.CorrelationId = "corr-batch";
        }, cancellationToken);

        await producer.Received(1).PublishBatchAsync(
            "products.events.v1",
            batch,
            Arg.Is<MessageMetadata>(metadata => metadata.CorrelationId == "corr-batch"),
            Arg.Any<CancellationToken>());
    }

    private sealed record TestPayload(string ProductId);
}
