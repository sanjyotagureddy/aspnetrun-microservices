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

            create table if not exists inventory_outbox
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

            create index if not exists ix_inventory_outbox_status_next_attempt on inventory_outbox (status, next_attempt_on_utc, occurred_on_utc);
            """,
            cancellationToken: cancellationToken));

        logger.LogInformation("Inventory schema is ready.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
