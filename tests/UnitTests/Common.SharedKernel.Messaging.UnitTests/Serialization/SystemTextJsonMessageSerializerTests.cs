namespace Common.SharedKernel.Messaging.UnitTests.Serialization;

public sealed class SystemTextJsonMessageSerializerTests
{
    [Fact]
    public void RoundTrip_ShouldPreserveEnvelopeMetadataAndPayload()
    {
        MessageMetadata metadata = new()
        {
            MessageId = "msg-1",
            CorrelationId = "corr-1",
            CausationId = "cause-1",
            TenantId = "tenant-1"
        };
        metadata.Headers["Source"] = "OrderService";
        MessageEnvelope<TestPayload> envelope = MessageEnvelope<TestPayload>.Create("orders.created", new TestPayload("ORD-1"), metadata);
        SystemTextJsonMessageSerializer serializer = new();

        byte[] payload = serializer.Serialize(envelope);
        IMessageEnvelope<TestPayload> restored = serializer.Deserialize<TestPayload>(payload);

        restored.MessageId.Should().Be("msg-1");
        restored.CorrelationId.Should().Be("corr-1");
        restored.Headers["Source"].Should().Be("OrderService");
        restored.Payload.OrderId.Should().Be("ORD-1");
    }

    private sealed record TestPayload(string OrderId);
}
