namespace Products.Api.Infrastructure;

internal interface IProductCatalogStore
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<ProductSearchResult> SearchAsync(ProductSearchFilter filter, CancellationToken cancellationToken);

    Task AddAsync(Product product, CancellationToken cancellationToken);

    Task UpdateAsync(Product product, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task EnsureSkuIsUniqueAsync(string sku, Guid? productId, CancellationToken cancellationToken);
}
