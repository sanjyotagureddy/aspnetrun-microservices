namespace Common.SharedKernel.Messaging.UnitTests.Metadata;

public sealed class MessageMetadataTests
{
    [Fact]
    public void Clone_ShouldCopyHeadersWithoutSharingDictionary()
    {
        MessageMetadata metadata = new() { CorrelationId = "corr-1" };
        metadata.Headers["Source"] = "OrderService";

        MessageMetadata clone = metadata.Clone();
        clone.Headers["Source"] = "InventoryService";

        metadata.Headers["Source"].Should().Be("OrderService");
        clone.CorrelationId.Should().Be("corr-1");
    }
}
