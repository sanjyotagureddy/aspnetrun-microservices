namespace Products.Api.Infrastructure;

using Npgsql;

internal interface IProductCatalogStore
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ProductSearchResult> SearchAsync(ProductSearchFilter filter, CancellationToken cancellationToken);

    Task AddAsync(Product product, CancellationToken cancellationToken);

    Task AddAsync(Product product, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken);

    Task UpdateAsync(Product product, CancellationToken cancellationToken);

    Task UpdateAsync(Product product, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken);

    Task EnsureSkuIsUniqueAsync(string sku, Guid? productId, CancellationToken cancellationToken);

    Task EnsureSkuIsUniqueAsync(string sku, Guid? productId, NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken);
}
