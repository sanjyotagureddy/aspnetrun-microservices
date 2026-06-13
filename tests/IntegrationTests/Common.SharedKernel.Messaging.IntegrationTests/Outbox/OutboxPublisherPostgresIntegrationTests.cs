using Common.SharedKernel.Logging;
using Common.SharedKernel.Messaging.IntegrationTests.Fixtures;
using Common.SharedKernel.Messaging.IntegrationTests.Support;
using Npgsql;

namespace Common.SharedKernel.Messaging.IntegrationTests.Outbox;

public sealed class OutboxPublisherPostgresIntegrationTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture = fixture;

    [Fact]
    public async Task Publisher_ShouldPublishAndMarkRowsAsPublished()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await using NpgsqlDataSource dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        await PostgresOutboxStore.EnsureSchemaAsync(dataSource, cancellationToken);

        PostgresOutboxStore store = new(dataSource);
        await store.EnqueueAsync(CreateMessage("product.created"), cancellationToken);
        await store.EnqueueAsync(CreateMessage("product.updated"), cancellationToken);

        var logger = new RecordingLogger<TestOutboxPublisher>();
        TestOutboxPublisher publisher = new(
            store,
            logger,
            publish: (_, _) => Task.CompletedTask,
            batchSize: 10,
            idleDelay: TimeSpan.FromMilliseconds(50));

        using var runCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        Task runTask = publisher.RunAsync(runCancellation.Token);

        await WaitForConditionAsync(async () =>
            await CountByStatusAsync(dataSource, "published", cancellationToken) == 2,
            timeout: TimeSpan.FromSeconds(10),
            cancellationToken);

        runCancellation.Cancel();
        await runTask;

        int pending = await CountByStatusAsync(dataSource, "pending", cancellationToken);
        int published = await CountByStatusAsync(dataSource, "published", cancellationToken);

        pending.Should().Be(0);
        published.Should().Be(2);
        logger.ErrorEntries.Should().BeEmpty();
    }

    [Fact]
    public async Task Publisher_ShouldMarkRowFailedWithIncrementedAttempt_WhenPublishThrows()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await using NpgsqlDataSource dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        await PostgresOutboxStore.EnsureSchemaAsync(dataSource, cancellationToken);

        PostgresOutboxStore store = new(dataSource);
        TestOutboxMessage message = CreateMessage("product.created");
        await store.EnqueueAsync(message, cancellationToken);

        var logger = new RecordingLogger<TestOutboxPublisher>();
        TestOutboxPublisher publisher = new(
            store,
            logger,
            publish: (_, _) => throw new InvalidOperationException("publish boom"),
            batchSize: 10,
            idleDelay: TimeSpan.FromMilliseconds(50));

        using var runCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        Task runTask = publisher.RunAsync(runCancellation.Token);

        await WaitForConditionAsync(async () =>
            await HasFailedAttemptAsync(dataSource, message.Id, expectedAttempt: 1, cancellationToken),
            timeout: TimeSpan.FromSeconds(10),
            cancellationToken);

        runCancellation.Cancel();
        await runTask;

        (ErrorLog Log, LogType Destination) errorEntry = logger.ErrorEntries.Should().ContainSingle().Subject;
        errorEntry.Log.Category.Should().Be("messaging.outbox.publish");
        errorEntry.Log.Context.Should().NotBeNull();
        errorEntry.Log.Context!.TryGetValue("eventType", out object? eventType).Should().BeTrue();
        eventType?.ToString().Should().Be(message.EventType);
    }

    private static TestOutboxMessage CreateMessage(string eventType)
        => new()
        {
            Id = Guid.NewGuid(),
            OccurredOnUtc = DateTime.UtcNow,
            EventType = eventType,
            Topic = "catalog.events",
            PayloadJson = "{}",
            MetadataJson = "{}",
            AttemptCount = 0
        };

    private static async Task<int> CountByStatusAsync(NpgsqlDataSource dataSource, string status, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "select count(*) from messaging_test_outbox where status = @status;";
        command.Parameters.AddWithValue("status", status);

        object? scalar = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(scalar);
    }

    private static async Task<bool> HasFailedAttemptAsync(NpgsqlDataSource dataSource, Guid id, int expectedAttempt, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            select attempt_count, last_error
            from messaging_test_outbox
            where id = @id and status = 'pending';
            """;
        command.Parameters.AddWithValue("id", id);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return false;
        }

        int attempt = reader.GetInt32(0);
        string? error = reader.IsDBNull(1) ? null : reader.GetString(1);
        return attempt == expectedAttempt && !string.IsNullOrWhiteSpace(error);
    }

    private static async Task WaitForConditionAsync(Func<Task<bool>> condition, TimeSpan timeout, CancellationToken cancellationToken)
    {
        DateTime deadline = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(150), cancellationToken);
        }

        throw new TimeoutException($"Condition was not met within {timeout.TotalSeconds} seconds.");
    }
}