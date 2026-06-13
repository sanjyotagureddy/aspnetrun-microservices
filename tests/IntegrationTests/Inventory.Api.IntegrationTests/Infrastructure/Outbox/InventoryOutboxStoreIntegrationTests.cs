using Dapper;
using Inventory.Api.Infrastructure;
using Inventory.Api.Infrastructure.Outbox;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;

namespace Inventory.Api.IntegrationTests.Infrastructure.Outbox;

public sealed class InventoryOutboxStoreIntegrationTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture = fixture;

    [Fact]
    public async Task ClaimPendingAsync_ShouldReclaimExpiredProcessingRows()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await using NpgsqlDataSource dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        InventorySchemaInitializer initializer = new(dataSource, new Mock<ILogger<InventorySchemaInitializer>>().Object);
        await initializer.StartAsync(cancellationToken);

        Guid pendingId = Guid.NewGuid();
        Guid expiredLeaseId = Guid.NewGuid();
        Guid activeLeaseId = Guid.NewGuid();

        await InsertOutboxRowAsync(dataSource, pendingId, "pending", null, cancellationToken);
        await InsertOutboxRowAsync(dataSource, expiredLeaseId, "processing", DateTime.UtcNow.AddMinutes(-5), cancellationToken);
        await InsertOutboxRowAsync(dataSource, activeLeaseId, "processing", DateTime.UtcNow.AddMinutes(5), cancellationToken);

        InventoryOutboxStore store = new(dataSource);

        IReadOnlyList<InventoryOutboxMessage> claimed = await store.ClaimPendingAsync(
            batchSize: 10,
            claimDuration: TimeSpan.FromSeconds(30),
            cancellationToken);

        HashSet<Guid> claimedIds = claimed.Select(message => message.Id).ToHashSet();
        claimedIds.Should().Contain(pendingId);
        claimedIds.Should().Contain(expiredLeaseId);
        claimedIds.Should().NotContain(activeLeaseId);

        (string Status, DateTime? NextAttemptOnUtc, DateTime? ProcessedOnUtc) pendingState =
            await GetOutboxStateAsync(dataSource, pendingId, cancellationToken);
        (string Status, DateTime? NextAttemptOnUtc, DateTime? ProcessedOnUtc) expiredState =
            await GetOutboxStateAsync(dataSource, expiredLeaseId, cancellationToken);
        (string Status, DateTime? NextAttemptOnUtc, DateTime? ProcessedOnUtc) activeLeaseState =
            await GetOutboxStateAsync(dataSource, activeLeaseId, cancellationToken);

        pendingState.Status.Should().Be("processing");
        pendingState.NextAttemptOnUtc.Should().NotBeNull();
        pendingState.NextAttemptOnUtc.Should().BeAfter(DateTime.UtcNow);
        pendingState.ProcessedOnUtc.Should().BeNull();

        expiredState.Status.Should().Be("processing");
        expiredState.NextAttemptOnUtc.Should().NotBeNull();
        expiredState.NextAttemptOnUtc.Should().BeAfter(DateTime.UtcNow);
        expiredState.ProcessedOnUtc.Should().BeNull();

        activeLeaseState.Status.Should().Be("processing");
        activeLeaseState.NextAttemptOnUtc.Should().NotBeNull();
        activeLeaseState.NextAttemptOnUtc.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task MarkPublishedAsync_ShouldClearLeaseAndSetProcessedTimestamp()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await using NpgsqlDataSource dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        InventorySchemaInitializer initializer = new(dataSource, new Mock<ILogger<InventorySchemaInitializer>>().Object);
        await initializer.StartAsync(cancellationToken);

        Guid id = Guid.NewGuid();
        await InsertOutboxRowAsync(dataSource, id, "processing", DateTime.UtcNow.AddMinutes(5), cancellationToken);

        InventoryOutboxStore store = new(dataSource);
        await store.MarkPublishedAsync(id, cancellationToken);

        (string Status, DateTime? NextAttemptOnUtc, DateTime? ProcessedOnUtc) state =
            await GetOutboxStateAsync(dataSource, id, cancellationToken);

        state.Status.Should().Be("published");
        state.NextAttemptOnUtc.Should().BeNull();
        state.ProcessedOnUtc.Should().NotBeNull();
    }

    private static async Task InsertOutboxRowAsync(
        NpgsqlDataSource dataSource,
        Guid id,
        string status,
        DateTime? nextAttemptOnUtc,
        CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into inventory_outbox
                (id, occurred_on_utc, event_type, topic, payload_json, metadata_json, status, attempt_count, next_attempt_on_utc)
            values
                (@Id, @OccurredOnUtc, @EventType, @Topic, cast(@PayloadJson as jsonb), cast(@MetadataJson as jsonb), @Status, 0, @NextAttemptOnUtc);
            """,
            new
            {
                Id = id,
                OccurredOnUtc = DateTime.UtcNow,
                EventType = "inventory.initialized",
                Topic = "inventory.events.v1",
                PayloadJson = "{}",
                MetadataJson = "{}",
                Status = status,
                NextAttemptOnUtc = nextAttemptOnUtc
            },
            cancellationToken: cancellationToken));
    }

    private static async Task<(string Status, DateTime? NextAttemptOnUtc, DateTime? ProcessedOnUtc)> GetOutboxStateAsync(
        NpgsqlDataSource dataSource,
        Guid id,
        CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        return await connection.QuerySingleAsync<(string Status, DateTime? NextAttemptOnUtc, DateTime? ProcessedOnUtc)>(new CommandDefinition(
            """
            select status as Status,
                   next_attempt_on_utc as NextAttemptOnUtc,
                   processed_on_utc as ProcessedOnUtc
            from inventory_outbox
            where id = @Id;
            """,
            new { Id = id },
            cancellationToken: cancellationToken));
    }
}
