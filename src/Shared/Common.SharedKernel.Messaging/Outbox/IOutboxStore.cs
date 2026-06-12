using Npgsql;

namespace Common.SharedKernel.Messaging.Outbox;

public interface IOutboxStore<TOutboxMessage>
    where TOutboxMessage : OutboxMessage
{
    Task EnqueueAsync(TOutboxMessage message, CancellationToken cancellationToken);

    Task EnqueueAsync(TOutboxMessage message, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken);

    Task<IReadOnlyList<TOutboxMessage>> ClaimPendingAsync(int batchSize, TimeSpan claimDuration, CancellationToken cancellationToken);

    Task MarkPublishedAsync(Guid id, CancellationToken cancellationToken);

    Task MarkFailedAsync(Guid id, int attemptCount, string error, CancellationToken cancellationToken);
}
