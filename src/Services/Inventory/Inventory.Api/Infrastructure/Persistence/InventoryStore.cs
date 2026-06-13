using Dapper;
using Npgsql;

namespace Inventory.Api.Infrastructure.Persistence;

internal sealed class InventoryStore(NpgsqlDataSource dataSource) : IInventoryStore
{
    public async Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        InventoryItemRecord? record = await connection.QuerySingleOrDefaultAsync<InventoryItemRecord>(
            new CommandDefinition(
                """
                select
                    product_id as ProductId,
                    stock_quantity as StockQuantity,
                    created_at as CreatedAt,
                    updated_at as UpdatedAt
                from inventory_items
                where product_id = @ProductId
                """,
                new { ProductId = productId },
                cancellationToken: cancellationToken));

        return record?.ToDomain();
    }

    public async Task<IReadOnlyDictionary<Guid, int>> GetStockByProductIdsAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        Guid[] distinctProductIds = productIds.Distinct().ToArray();

        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        InventoryStockRecord[] rows = (await connection.QueryAsync<InventoryStockRecord>(
            new CommandDefinition(
                """
                select
                    product_id as ProductId,
                    stock_quantity as StockQuantity
                from inventory_items
                where product_id = any(@ProductIds)
                """,
                new { ProductIds = distinctProductIds },
                cancellationToken: cancellationToken))).ToArray();

        return rows.ToDictionary(row => row.ProductId, row => row.StockQuantity);
    }

    public async Task InitializeAsync(InventoryItem item, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await InitializeInternalAsync(item, connection, null, cancellationToken);
    }

    public Task InitializeAsync(InventoryItem item, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken)
        => InitializeInternalAsync(item, connection, transaction, cancellationToken);

    private static async Task InitializeInternalAsync(InventoryItem item, NpgsqlConnection connection, NpgsqlTransaction? transaction, CancellationToken cancellationToken)
    {
        await connection.ExecuteAsync(new CommandDefinition(
            """
            insert into inventory_items (product_id, stock_quantity, created_at, updated_at)
            values (@ProductId, @StockQuantity, @CreatedAt, @UpdatedAt)
            on conflict (product_id)
            do update set
                stock_quantity = excluded.stock_quantity,
                updated_at = excluded.updated_at;
            """,
            item.ToRecord(),
            transaction,
            cancellationToken: cancellationToken));
    }

    private sealed class InventoryStockRecord
    {
        public Guid ProductId { get; set; }

        public int StockQuantity { get; set; }
    }
}
