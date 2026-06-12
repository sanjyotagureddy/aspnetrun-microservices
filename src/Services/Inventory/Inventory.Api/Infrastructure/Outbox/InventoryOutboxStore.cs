using Dapper;
using Npgsql;

namespace Inventory.Api.Infrastructure.Outbox;

internal sealed class InventoryOutboxStore(NpgsqlDataSource dataSource) : IInventoryOutboxStore
{
    public async Task EnqueueAsync(InventoryOutboxMessage message, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
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
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyList<InventoryOutboxMessage>> GetPendingAsync(int batchSize, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        IEnumerable<InventoryOutboxMessage> rows = await connection.QueryAsync<InventoryOutboxMessage>(new CommandDefinition(
            """
            select id,
                   occurred_on_utc as OccurredOnUtc,
                   event_type as EventType,
                   topic as Topic,
                   payload_json::text as PayloadJson,
                   metadata_json::text as MetadataJson,
                   attempt_count as AttemptCount
            from inventory_outbox
            where status = 'pending'
              and (next_attempt_on_utc is null or next_attempt_on_utc <= now())
            order by occurred_on_utc asc
            limit @BatchSize
            """,
            new { BatchSize = batchSize },
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
            set attempt_count = @AttemptCount,
                last_error = @Error,
                next_attempt_on_utc = now() + make_interval(secs => @NextAttemptSeconds)
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
}
