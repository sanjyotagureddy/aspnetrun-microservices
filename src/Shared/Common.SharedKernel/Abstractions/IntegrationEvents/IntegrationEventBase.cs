namespace Common.SharedKernel.Abstractions.IntegrationEvents;

public abstract record IntegrationEventBase(Guid EventId, DateTime OccurredOnUtc) : IIntegrationEvent
{
    public abstract string EventType { get; }

    protected IntegrationEventBase()
        : this(Guid.NewGuid(), DateTime.UtcNow)
    {
    }
}