using Dapper;
using Npgsql;

namespace Products.Api.Infrastructure.Persistence;

internal sealed class ProductCatalogStore(NpgsqlDataSource dataSource) : Products.Api.Infrastructure.IProductCatalogStore
{
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        ProductRecord? record = await connection.QuerySingleOrDefaultAsync<ProductRecord>(
            new CommandDefinition(
                """
                select id, name, description, sku, price, currency, category, brand, stock_quantity as StockQuantity, is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt
                from products
                where id = @Id
                """,
                new { Id = id },
                cancellationToken: cancellationToken));

        return record?.ToDomain();
    }

    public async Task<ProductSearchResult> SearchAsync(ProductSearchFilter filter, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        DynamicParameters parameters = new();
        var whereClause = BuildWhereClause(filter, parameters);
        parameters.Add("Limit", filter.PageSize);
        parameters.Add("Offset", (filter.Page - 1) * filter.PageSize);

        var countSql = $"select count(*) from products{whereClause};";
        var itemsSql =
            $"""
            select id, name, description, sku, price, currency, category, brand, stock_quantity as StockQuantity, is_active as IsActive, created_at as CreatedAt, updated_at as UpdatedAt
            from products
            {whereClause}
            order by name asc, sku asc
            limit @Limit offset @Offset;
            """;

        var totalCount = await connection.ExecuteScalarAsync<long>(new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));
        IEnumerable<ProductRecord> records = await connection.QueryAsync<ProductRecord>(new CommandDefinition(itemsSql, parameters, cancellationToken: cancellationToken));
        var materializedRecords = records.ToList();

        return new ProductSearchResult(materializedRecords.Select(record => record.ToDomain()).ToArray(), (int)totalCount);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                """
                insert into products (id, name, description, sku, price, currency, category, brand, stock_quantity, is_active, created_at, updated_at)
                values (@Id, @Name, @Description, @Sku, @Price, @Currency, @category, @Brand, @StockQuantity, @IsActive, @CreatedAt, @UpdatedAt)
                """,
                product.ToRecord(),
                cancellationToken: cancellationToken));
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new Common.SharedKernel.Exceptions.ConflictException($"Product SKU '{product.Sku}' already exists.");
        }
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        try
        {
            var affectedRows = await connection.ExecuteAsync(new CommandDefinition(
                """
                update products
                set name = @Name,
                    description = @Description,
                    sku = @Sku,
                    price = @Price,
                    currency = @Currency,
                    category = @Category,
                    brand = @Brand,
                    stock_quantity = @StockQuantity,
                    is_active = @IsActive,
                    updated_at = @UpdatedAt
                where id = @Id
                """,
                product.ToRecord(),
                cancellationToken: cancellationToken));

            if (affectedRows == 0)
            {
                throw new Common.SharedKernel.Exceptions.NotFoundException(nameof(Product), product.Id.ToString());
            }
        }
        catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            throw new Common.SharedKernel.Exceptions.ConflictException($"Product SKU '{product.Sku}' already exists.");
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var affectedRows = await connection.ExecuteAsync(new CommandDefinition(
            "delete from products where id = @Id",
            new { Id = id },
            cancellationToken: cancellationToken));

        return affectedRows > 0;
    }

    public async Task EnsureSkuIsUniqueAsync(string sku, Guid? productId, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);
        var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            """
            select exists(
                select 1
                from products
                where sku = @Sku and (@ProductId is null or id <> @ProductId)
            )
            """,
            new { Sku = sku, ProductId = productId },
            cancellationToken: cancellationToken));

        if (exists)
        {
            throw new Common.SharedKernel.Exceptions.ConflictException($"Product SKU '{sku}' already exists.");
        }
    }

    private static string BuildWhereClause(ProductSearchFilter filter, DynamicParameters parameters)
    {
        List<string> conditions = [];

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            conditions.Add("(name ilike @Search or description ilike @Search or sku ilike @Search)");
            parameters.Add("Search", $"%{filter.Search.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            conditions.Add("category ilike @Namespace");
            parameters.Add("Namespace", $"%{filter.Category.Trim()}%");
        }

        if (!string.IsNullOrWhiteSpace(filter.Brand))
        {
            conditions.Add("brand ilike @Brand");
            parameters.Add("Brand", $"%{filter.Brand.Trim()}%");
        }

        if (filter.IsActive.HasValue)
        {
            conditions.Add("is_active = @IsActive");
            parameters.Add("IsActive", filter.IsActive.Value);
        }

        return conditions.Count == 0 ? string.Empty : $" where {string.Join(" and ", conditions)}";
    }
}
