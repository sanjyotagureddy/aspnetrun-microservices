namespace Common.SharedKernel.Abstractions.Events;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }

    string EventType { get; }
}