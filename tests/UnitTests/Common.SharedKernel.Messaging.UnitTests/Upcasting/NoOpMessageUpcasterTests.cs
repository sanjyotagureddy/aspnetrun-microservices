namespace Common.SharedKernel.Messaging.UnitTests.Upcasting;

public sealed class NoOpMessageUpcasterTests
{
    [Fact]
    public void CanUpcast_ShouldAlwaysReturnFalse()
    {
        NoOpMessageUpcaster upcaster = new();

        bool result = upcaster.CanUpcast(
            new MessageContractDescriptor("TypeA", "1.0", "application/json"),
            new MessageContractDescriptor("TypeA", "2.0", "application/json"),
            typeof(TestPayload));

        result.Should().BeFalse();
    }

    [Fact]
    public void Upcast_ShouldReturnSameEnvelopeInstance()
    {
        NoOpMessageUpcaster upcaster = new();
        MessageMetadata metadata = new();
        MessageEnvelope<TestPayload> envelope = MessageEnvelope<TestPayload>.Create("products.events.v1", new TestPayload("P-1"), metadata);

        IMessageEnvelope<TestPayload> upcasted = upcaster.Upcast(envelope, new MessageContractDescriptor("TypeA", "2.0", "application/json"));

        upcasted.Should().BeSameAs(envelope);
    }

    private sealed record TestPayload(string ProductId);
}
