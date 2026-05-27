namespace Common.SharedKernel.Abstractions.IntegrationEvents;

public abstract record IntegrationEventBase(Guid EventId, DateTime OccurredOnUtc) : IIntegrationEvent
{
    protected IntegrationEventBase()
        : this(Guid.NewGuid(), DateTime.UtcNow)
    {
    }
}