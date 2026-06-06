namespace Common.SharedKernel.Messaging;

public sealed class DestinationRegistration
{
    public string DestinationName { get; set; } = string.Empty;

    public DestinationKind Kind { get; set; } = DestinationKind.Topic;

    public string OwnerService { get; set; } = string.Empty;

    public MessageContractDescriptor Contract { get; set; } = MessageContractDescriptor.Unspecified;

    public PartitioningStrategy PartitioningStrategy { get; set; } = PartitioningStrategy.ByOrderingKey;

    // Logical key selector descriptor (for example: payload.productId).
    public string PartitionKeySelector { get; set; } = string.Empty;

    public int? PartitionCount { get; set; }

    public TimeSpan? Retention { get; set; }

    public string? DeadLetterDestination { get; set; }

    public bool OrderingRequired { get; set; } = true;

    public DestinationRegistration Clone() => new()
    {
        DestinationName = DestinationName,
        Kind = Kind,
        OwnerService = OwnerService,
        Contract = Contract,
        PartitioningStrategy = PartitioningStrategy,
        PartitionKeySelector = PartitionKeySelector,
        PartitionCount = PartitionCount,
        Retention = Retention,
        DeadLetterDestination = DeadLetterDestination,
        OrderingRequired = OrderingRequired
    };
}
