using Common.SharedKernel.Messaging.Outbox;
using Dapper;
using Npgsql;

namespace Inventory.Api.Infrastructure.Outbox;

internal sealed class InventoryOutboxStore(NpgsqlDataSource dataSource) : IInventoryOutboxStore
{
    public async Task EnqueueAsync(InventoryOutboxMessage message, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await EnqueueInternalAsync(message, connection, null, cancellationToken);
    }

    public Task EnqueueAsync(InventoryOutboxMessage message, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
        => EnqueueInternalAsync(message, connection, transaction, cancellationToken);

    private static async Task EnqueueInternalAsync(InventoryOutboxMessage message, NpgsqlConnection connection, NpgsqlTransaction? transaction, CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into inventory_outbox (id, occurred_on_utc, event_type, topic, payload_json, metadata_json, status, attempt_count)
            values (@Id, @OccurredOnUtc, @EventType, @Topic, cast(@PayloadJson as jsonb), cast(@MetadataJson as jsonb), 'pending', 0)
            """,
            new
            {
                message.Id,
                message.OccurredOnUtc,
                message.EventType,
                message.Topic,
                message.PayloadJson,
                message.MetadataJson
            },
            transaction,
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<InventoryOutboxMessage>> ClaimPendingAsync(int batchSize, TimeSpan claimDuration, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        int claimSeconds = Math.Max(10, (int)claimDuration.TotalSeconds);

        IEnumerable<InventoryOutboxMessage> rows = await connection.QueryAsync<InventoryOutboxMessage>(new CommandDefinition(
            """
            with candidates as
            (
                select id
                from inventory_outbox
                                where (
                                                status = 'pending'
                                                and (next_attempt_on_utc is null or next_attempt_on_utc <= now())
                                            )
                                     or (
                                                status = 'processing'
                                                and next_attempt_on_utc <= now()
                                            )
                order by occurred_on_utc asc
                for update skip locked
                limit @BatchSize
            )
            update inventory_outbox as outbox
            set status = 'processing',
                                next_attempt_on_utc = now() + make_interval(secs => @ClaimSeconds),
                                processed_on_utc = null
            from candidates
            where outbox.id = candidates.id
            returning outbox.id,
                      outbox.occurred_on_utc as OccurredOnUtc,
                      outbox.event_type as EventType,
                      outbox.topic as Topic,
                      outbox.payload_json::text as PayloadJson,
                      outbox.metadata_json::text as MetadataJson,
                      outbox.attempt_count as AttemptCount
            """,
            new
            {
                BatchSize = batchSize,
                ClaimSeconds = claimSeconds
            },
            cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task MarkPublishedAsync(Guid id, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            """
            update inventory_outbox
            set status = 'published',
                processed_on_utc = now(),
                next_attempt_on_utc = null,
                last_error = null
            where id = @Id
            """,
            new { Id = id },
            cancellationToken: cancellationToken));
    }

    public async Task MarkFailedAsync(Guid id, int attemptCount, string error, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        int nextAttemptSeconds = Math.Min(300, Math.Max(5, (int)Math.Pow(2, attemptCount)));

        await connection.ExecuteAsync(new CommandDefinition(
            """
            update inventory_outbox
            set status = 'pending',
                attempt_count = @AttemptCount,
                last_error = @Error,
                next_attempt_on_utc = now() + make_interval(secs => @NextAttemptSeconds),
                processed_on_utc = null
            where id = @Id
            """,
            new
            {
                Id = id,
                AttemptCount = attemptCount,
                Error = error,
                NextAttemptSeconds = nextAttemptSeconds
            },
            cancellationToken: cancellationToken));
    }

    public async Task<OutboxBacklogSnapshot> GetBacklogSnapshotAsync(CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        BacklogRow row = await connection.QuerySingleAsync<BacklogRow>(new CommandDefinition(
            """
            select
                count(*) filter (where status = 'pending' and (next_attempt_on_utc is null or next_attempt_on_utc <= now())) as PendingReadyCount,
                count(*) filter (where status = 'processing' and next_attempt_on_utc <= now()) as StaleProcessingCount,
                min(occurred_on_utc) filter (where status = 'pending' and (next_attempt_on_utc is null or next_attempt_on_utc <= now())) as OldestPendingOccurredOnUtc
            from inventory_outbox
            """,
            cancellationToken: cancellationToken));

        return new OutboxBacklogSnapshot(
            row.PendingReadyCount,
            row.StaleProcessingCount,
            row.OldestPendingOccurredOnUtc);
    }

    private sealed record BacklogRow(int PendingReadyCount, int StaleProcessingCount, DateTime? OldestPendingOccurredOnUtc);
}
