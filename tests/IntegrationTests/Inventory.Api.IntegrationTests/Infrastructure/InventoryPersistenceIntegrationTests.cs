using Dapper;
using Inventory.Api.Domain;
using Inventory.Api.Infrastructure;
using Inventory.Api.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;

namespace Inventory.Api.IntegrationTests.Infrastructure;

public sealed class InventoryPersistenceIntegrationTests : IClassFixture<PostgresFixture>
{
    private readonly PostgresFixture _fixture;

    public InventoryPersistenceIntegrationTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SchemaInitializer_ShouldCreateInventoryTable_AndIndex()
    {
        await using NpgsqlDataSource dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        Mock<ILogger<InventorySchemaInitializer>> logger = new();
        InventorySchemaInitializer initializer = new(dataSource, logger.Object);

        await initializer.StartAsync(TestContext.Current.CancellationToken);

        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(TestContext.Current.CancellationToken);
        int tableCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "select count(*) from information_schema.tables where table_name = 'inventory_items';",
            cancellationToken: TestContext.Current.CancellationToken));

        int indexCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "select count(*) from pg_indexes where tablename = 'inventory_items' and indexname = 'ix_inventory_items_updated_at';",
            cancellationToken: TestContext.Current.CancellationToken));

        Assert.Equal(1, tableCount);
        Assert.Equal(1, indexCount);
    }

    [Fact]
    public async Task InventoryStore_ShouldInitializeAndQueryInventory()
    {
        await using NpgsqlDataSource dataSource = NpgsqlDataSource.Create(_fixture.ConnectionString);
        Mock<ILogger<InventorySchemaInitializer>> logger = new();
        InventorySchemaInitializer initializer = new(dataSource, logger.Object);
        await initializer.StartAsync(TestContext.Current.CancellationToken);

        InventoryStore store = new(dataSource);
        Guid productId = Guid.NewGuid();
        DateTime createdAtUtc = DateTime.UtcNow;
        InventoryItem item = new(productId, 14, createdAtUtc);

        await store.InitializeAsync(item, TestContext.Current.CancellationToken);

        InventoryItem? loaded = await store.GetByProductIdAsync(productId, TestContext.Current.CancellationToken);
        Assert.NotNull(loaded);
        Assert.Equal(14, loaded.StockQuantity);

        IReadOnlyDictionary<Guid, int> stocks = await store.GetStockByProductIdsAsync([productId, Guid.NewGuid()], TestContext.Current.CancellationToken);
        Assert.True(stocks.ContainsKey(productId));
        Assert.Equal(14, stocks[productId]);
    }
}
