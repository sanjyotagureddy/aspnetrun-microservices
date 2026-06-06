namespace Common.SharedKernel.Messaging.UnitTests.Metadata;

public sealed class MessageContextTests
{
    [Fact]
    public void MessageContext_ShouldExposeAssignedValues()
    {
        IReadOnlyDictionary<string, string> headers = new Dictionary<string, string>
        {
            ["EventType"] = "ProductCreatedIntegrationEvent"
        };

        MessageContext context = new(
            MessageId: "msg-1",
            CorrelationId: "corr-1",
            TraceId: "trace-1",
            SpanId: "span-1",
            TenantId: "tenant-1",
            Topic: "products.events.v1",
            Partition: 2,
            Offset: 42,
            Headers: headers);

        context.MessageId.Should().Be("msg-1");
        context.CorrelationId.Should().Be("corr-1");
        context.TraceId.Should().Be("trace-1");
        context.SpanId.Should().Be("span-1");
        context.TenantId.Should().Be("tenant-1");
        context.Topic.Should().Be("products.events.v1");
        context.Partition.Should().Be(2);
        context.Offset.Should().Be(42);
        context.Headers["EventType"].Should().Be("ProductCreatedIntegrationEvent");
    }
}
