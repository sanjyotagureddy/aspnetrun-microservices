namespace Common.SharedKernel.Messaging;

public enum PartitioningStrategy
{
    None = 0,
    ByAggregateId = 1,
    ByOrderingKey = 2,
    ByRoutingKey = 3,
    ExplicitPartition = 4
}
