using Dapper;
using Npgsql;

namespace Products.Api.Infrastructure;

internal sealed class ProductCatalogSchemaInitializer(NpgsqlDataSource dataSource, ILogger<ProductCatalogSchemaInitializer> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(new CommandDefinition(
            """
            create table if not exists products
            (
                id uuid primary key,
                name text not null,
                description text not null,
                sku text not null unique,
                price numeric(18,2) not null,
                currency varchar(3) not null,
                category text not null,
                brand text not null,
                stock_quantity integer not null,
                is_active boolean not null,
                created_at timestamptz not null,
                updated_at timestamptz not null
            );

            create index if not exists ix_products_name on products (name);
            create index if not exists ix_products_category on products (category);
            create index if not exists ix_products_brand on products (brand);
            create index if not exists ix_products_is_active on products (is_active);

            create table if not exists product_outbox
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

            create index if not exists ix_product_outbox_status_next_attempt on product_outbox (status, next_attempt_on_utc, occurred_on_utc);
            """,
            cancellationToken: cancellationToken));

        logger.LogInformation("Product catalog schema is ready.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}