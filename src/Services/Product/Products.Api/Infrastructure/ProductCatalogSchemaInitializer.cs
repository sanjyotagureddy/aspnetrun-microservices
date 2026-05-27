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
            """,
            cancellationToken: cancellationToken));

        logger.LogInformation("Product catalog schema is ready.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}