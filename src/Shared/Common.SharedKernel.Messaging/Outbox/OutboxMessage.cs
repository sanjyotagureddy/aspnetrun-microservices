namespace Common.SharedKernel.Messaging.Outbox;

public class OutboxMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public DateTime OccurredOnUtc { get; init; }

    public string EventType { get; init; } = string.Empty;

    public string Topic { get; init; } = string.Empty;

    public string PayloadJson { get; init; } = string.Empty;

    public string MetadataJson { get; init; } = string.Empty;

    public int AttemptCount { get; init; }
}
