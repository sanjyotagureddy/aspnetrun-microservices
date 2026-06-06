using Dapper;
using Npgsql;

namespace Inventory.Api.Infrastructure;

internal sealed class InventorySchemaInitializer(NpgsqlDataSource dataSource, ILogger<InventorySchemaInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            """
            create table if not exists inventory_items
            (
                product_id uuid primary key,
                stock_quantity integer not null,
                created_at timestamptz not null,
                updated_at timestamptz not null,
                constraint ck_inventory_items_stock_quantity_non_negative check (stock_quantity >= 0)
            );

            create index if not exists ix_inventory_items_updated_at on inventory_items (updated_at);
            """,
            cancellationToken: cancellationToken));

        logger.LogInformation("Inventory schema is ready.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
