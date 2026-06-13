namespace Common.SharedKernel.Messaging.Outbox;

public interface IOutboxBacklogReader
{
    Task<OutboxBacklogSnapshot> GetBacklogSnapshotAsync(CancellationToken cancellationToken);
}
