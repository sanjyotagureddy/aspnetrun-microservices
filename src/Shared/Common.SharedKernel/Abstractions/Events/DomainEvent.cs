namespace Common.SharedKernel.Abstractions.Events;

public abstract record DomainEvent : IDomainEvent
{
    protected DomainEvent()
        : this(DateTime.UtcNow)
    {
    }

    protected DomainEvent(DateTime occurredOnUtc)
    {
        OccurredOnUtc = occurredOnUtc;
    }

    public DateTime OccurredOnUtc { get; }

    public virtual string EventType => GetType().Name;
}