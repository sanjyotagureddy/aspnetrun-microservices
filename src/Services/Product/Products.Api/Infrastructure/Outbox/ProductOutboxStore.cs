using Dapper;
using Npgsql;

namespace Products.Api.Infrastructure.Outbox;

internal sealed class ProductOutboxStore(NpgsqlDataSource dataSource) : IProductOutboxStore
{
    public async Task EnqueueAsync(ProductOutboxMessage message, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await EnqueueInternalAsync(message, connection, null, cancellationToken);
    }

    public Task EnqueueAsync(ProductOutboxMessage message, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
        => EnqueueInternalAsync(message, connection, transaction, cancellationToken);

    private static async Task EnqueueInternalAsync(ProductOutboxMessage message, NpgsqlConnection connection, NpgsqlTransaction? transaction, CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into product_outbox (id, occurred_on_utc, event_type, topic, payload_json, metadata_json, status, attempt_count)
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

    public async Task<IReadOnlyList<ProductOutboxMessage>> ClaimPendingAsync(int batchSize, TimeSpan claimDuration, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        int claimSeconds = Math.Max(10, (int)claimDuration.TotalSeconds);

        IEnumerable<ProductOutboxMessage> rows = await connection.QueryAsync<ProductOutboxMessage>(new CommandDefinition(
            """
            with candidates as
            (
                select id
                from product_outbox
                where status = 'pending'
                  and (next_attempt_on_utc is null or next_attempt_on_utc <= now())
                order by occurred_on_utc asc
                for update skip locked
                limit @BatchSize
            )
            update product_outbox as outbox
            set status = 'processing',
                next_attempt_on_utc = now() + make_interval(secs => @ClaimSeconds)
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
            update product_outbox
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
            update product_outbox
            set status = 'pending',
                attempt_count = @AttemptCount,
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
