using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using Products.Api.Infrastructure;
using Products.Api.Infrastructure.Outbox;
using Testcontainers.PostgreSql;

namespace Products.Api.Tests.Infrastructure.Outbox;

public sealed class ProductOutboxStoreTests(PostgresFixture fixture) : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture = fixture;

    [Fact]
    public async Task ClaimPendingAsync_ShouldReclaimExpiredProcessingRows()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;

        await using NpgsqlDataSource dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        ProductCatalogSchemaInitializer initializer = new(dataSource, new Mock<ILogger<ProductCatalogSchemaInitializer>>().Object);
        await initializer.StartAsync(cancellationToken);

        Guid pendingId = Guid.NewGuid();
        Guid expiredLeaseId = Guid.NewGuid();
        Guid activeLeaseId = Guid.NewGuid();

        await InsertOutboxRowAsync(dataSource, pendingId, "pending", null, cancellationToken);
        await InsertOutboxRowAsync(dataSource, expiredLeaseId, "processing", DateTime.UtcNow.AddMinutes(-5), cancellationToken);
        await InsertOutboxRowAsync(dataSource, activeLeaseId, "processing", DateTime.UtcNow.AddMinutes(5), cancellationToken);

        ProductOutboxStore store = new(dataSource);

        IReadOnlyList<ProductOutboxMessage> claimed = await store.ClaimPendingAsync(
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
        ProductCatalogSchemaInitializer initializer = new(dataSource, new Mock<ILogger<ProductCatalogSchemaInitializer>>().Object);
        await initializer.StartAsync(cancellationToken);

        Guid id = Guid.NewGuid();
        await InsertOutboxRowAsync(dataSource, id, "processing", DateTime.UtcNow.AddMinutes(5), cancellationToken);

        ProductOutboxStore store = new(dataSource);
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
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            insert into product_outbox
                (id, occurred_on_utc, event_type, topic, payload_json, metadata_json, status, attempt_count, next_attempt_on_utc)
            values
                (@id, @occurredOnUtc, @eventType, @topic, cast(@payloadJson as jsonb), cast(@metadataJson as jsonb), @status, 0, @nextAttemptOnUtc);
            """;
        command.Parameters.AddWithValue("id", id);
        command.Parameters.AddWithValue("occurredOnUtc", DateTime.UtcNow);
        command.Parameters.AddWithValue("eventType", "product.created");
        command.Parameters.AddWithValue("topic", "products.events.v1");
        command.Parameters.AddWithValue("payloadJson", "{}");
        command.Parameters.AddWithValue("metadataJson", "{}");
        command.Parameters.AddWithValue("status", status);
        command.Parameters.AddWithValue("nextAttemptOnUtc", (object?)nextAttemptOnUtc ?? DBNull.Value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<(string Status, DateTime? NextAttemptOnUtc, DateTime? ProcessedOnUtc)> GetOutboxStateAsync(
        NpgsqlDataSource dataSource,
        Guid id,
        CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText =
            """
            select status, next_attempt_on_utc, processed_on_utc
            from product_outbox
            where id = @id;
            """;
        command.Parameters.AddWithValue("id", id);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        (await reader.ReadAsync(cancellationToken)).Should().BeTrue();

        string status = reader.GetString(0);
        DateTime? nextAttemptOnUtc = reader.IsDBNull(1) ? null : reader.GetDateTime(1);
        DateTime? processedOnUtc = reader.IsDBNull(2) ? null : reader.GetDateTime(2);

        return (status, nextAttemptOnUtc, processedOnUtc);
    }
}

public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("products_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}
