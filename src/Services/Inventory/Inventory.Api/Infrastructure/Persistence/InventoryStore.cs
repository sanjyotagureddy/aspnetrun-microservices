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

    public async Task InitializeAsync(InventoryItem item, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
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
            cancellationToken: cancellationToken));
    }
}
