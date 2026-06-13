using Common.SharedKernel.Logging;
using Common.SharedKernel.Messaging.Outbox;
using Npgsql;

namespace Common.SharedKernel.Messaging.IntegrationTests.Support;

internal sealed class TestOutboxMessage : OutboxMessage;

internal sealed class PostgresOutboxStore(NpgsqlDataSource dataSource) : IOutboxStore<TestOutboxMessage>
{
    public static async Task EnsureSchemaAsync(NpgsqlDataSource dataSource, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            create table if not exists messaging_test_outbox
            (
                id uuid primary key,
                occurred_on_utc timestamptz not null,
                event_type text not null,
                topic text not null,
                payload_json jsonb not null,
                metadata_json jsonb not null,
                status text not null,
                attempt_count integer not null default 0,
                next_attempt_on_utc timestamptz null,
                processed_on_utc timestamptz null,
                last_error text null
            );

            create index if not exists ix_messaging_test_outbox_status_next_attempt
                on messaging_test_outbox (status, next_attempt_on_utc, occurred_on_utc);
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task EnqueueAsync(TestOutboxMessage message, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await EnqueueInternalAsync(message, connection, null, cancellationToken);
    }

    public Task EnqueueAsync(TestOutboxMessage message, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
        => EnqueueInternalAsync(message, connection, transaction, cancellationToken);

    private static async Task EnqueueInternalAsync(TestOutboxMessage message, NpgsqlConnection connection, NpgsqlTransaction? transaction, CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText =
            """
            insert into messaging_test_outbox (id, occurred_on_utc, event_type, topic, payload_json, metadata_json, status, attempt_count)
            values (@id, @occurredOnUtc, @eventType, @topic, cast(@payloadJson as jsonb), cast(@metadataJson as jsonb), 'pending', 0);
            """;
        command.Parameters.AddWithValue("id", message.Id);
        command.Parameters.AddWithValue("occurredOnUtc", message.OccurredOnUtc);
        command.Parameters.AddWithValue("eventType", message.EventType);
        command.Parameters.AddWithValue("topic", message.Topic);
        command.Parameters.AddWithValue("payloadJson", message.PayloadJson);
        command.Parameters.AddWithValue("metadataJson", message.MetadataJson);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TestOutboxMessage>> ClaimPendingAsync(int batchSize, TimeSpan claimDuration, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        int claimSeconds = Math.Max(10, (int)claimDuration.TotalSeconds);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            with candidates as
            (
                select id
                from messaging_test_outbox
                where status = 'pending'
                  and (next_attempt_on_utc is null or next_attempt_on_utc <= now())
                order by occurred_on_utc asc
                for update skip locked
                limit @batchSize
            )
            update messaging_test_outbox as outbox
            set status = 'processing',
                next_attempt_on_utc = now() + make_interval(secs => @claimSeconds)
            from candidates
            where outbox.id = candidates.id
            returning outbox.id,
                      outbox.occurred_on_utc,
                      outbox.event_type,
                      outbox.topic,
                      outbox.payload_json::text,
                      outbox.metadata_json::text,
                      outbox.attempt_count;
            """;
        command.Parameters.AddWithValue("batchSize", batchSize);
        command.Parameters.AddWithValue("claimSeconds", claimSeconds);

        List<TestOutboxMessage> claimed = [];
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            claimed.Add(new TestOutboxMessage
            {
                Id = reader.GetGuid(0),
                OccurredOnUtc = reader.GetDateTime(1),
                EventType = reader.GetString(2),
                Topic = reader.GetString(3),
                PayloadJson = reader.GetString(4),
                MetadataJson = reader.GetString(5),
                AttemptCount = reader.GetInt32(6)
            });
        }

        return claimed;
    }

    public async Task MarkPublishedAsync(Guid id, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            update messaging_test_outbox
            set status = 'published',
                processed_on_utc = now(),
                last_error = null
            where id = @id;
            """;
        command.Parameters.AddWithValue("id", id);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(Guid id, int attemptCount, string error, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        int nextAttemptSeconds = Math.Min(300, Math.Max(5, (int)Math.Pow(2, attemptCount)));

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            update messaging_test_outbox
            set status = 'pending',
                attempt_count = @attemptCount,
                last_error = @error,
                next_attempt_on_utc = now() + make_interval(secs => @nextAttemptSeconds)
            where id = @id;
            """;
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("attemptCount", attemptCount);
        command.Parameters.AddWithValue("error", error);
        command.Parameters.AddWithValue("nextAttemptSeconds", nextAttemptSeconds);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

internal sealed class TestOutboxPublisher(
    IOutboxStore<TestOutboxMessage> outboxStore,
    ILogger<TestOutboxPublisher> logger,
    Func<TestOutboxMessage, CancellationToken, Task> publish,
    int batchSize,
    TimeSpan? claimDuration = null,
    TimeSpan? idleDelay = null)
    : OutboxPublisherBase<TestOutboxMessage>(outboxStore, logger, batchSize, claimDuration, idleDelay)
{
    private readonly Func<TestOutboxMessage, CancellationToken, Task> _publish = publish;

    public Task RunAsync(CancellationToken cancellationToken)
        => ExecuteAsync(cancellationToken);

    protected override Task PublishAsync(TestOutboxMessage outboxMessage, CancellationToken cancellationToken)
        => _publish(outboxMessage, cancellationToken);
}