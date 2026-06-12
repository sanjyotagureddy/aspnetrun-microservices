namespace Common.SharedKernel.Abstractions.IntegrationEvents;

public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTime OccurredOnUtc { get; }

    string EventType { get; }
}