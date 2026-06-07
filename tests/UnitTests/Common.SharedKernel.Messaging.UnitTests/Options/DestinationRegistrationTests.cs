namespace Common.SharedKernel.Messaging.UnitTests.Options;

public sealed class DestinationRegistrationTests
{
    [Fact]
    public void Clone_ShouldCopyAllConfiguredProperties()
    {
        DestinationRegistration registration = new()
        {
            DestinationName = "products.events.v1",
            Kind = DestinationKind.Topic,
            OwnerService = "products-api",
            Contract = new MessageContractDescriptor("ProductLifecycleEvent", "1.0", "application/json"),
            PartitioningStrategy = PartitioningStrategy.ByAggregateId,
            PartitionKeySelector = "payload.productId",
            PartitionCount = 12,
            Retention = TimeSpan.FromDays(7),
            DeadLetterDestination = "products.events.dlq",
            OrderingRequired = true
        };

        DestinationRegistration clone = registration.Clone();

        clone.DestinationName.Should().Be("products.events.v1");
        clone.Kind.Should().Be(DestinationKind.Topic);
        clone.OwnerService.Should().Be("products-api");
        clone.Contract.MessageType.Should().Be("ProductLifecycleEvent");
        clone.Contract.Version.Should().Be("1.0");
        clone.PartitioningStrategy.Should().Be(PartitioningStrategy.ByAggregateId);
        clone.PartitionKeySelector.Should().Be("payload.productId");
        clone.PartitionCount.Should().Be(12);
        clone.Retention.Should().Be(TimeSpan.FromDays(7));
        clone.DeadLetterDestination.Should().Be("products.events.dlq");
        clone.OrderingRequired.Should().BeTrue();
        clone.Should().NotBeSameAs(registration);
    }
}
