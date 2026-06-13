namespace Common.SharedKernel.Messaging.Outbox;

public sealed record OutboxBacklogSnapshot(
    int PendingReadyCount,
    int StaleProcessingCount,
    DateTime? OldestPendingOccurredOnUtc);
