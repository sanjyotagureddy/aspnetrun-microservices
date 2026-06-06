namespace Products.Api.Infrastructure;

internal interface IInventoryStockAdapter
{
    Task InitializeAsync(Guid productId, int stockQuantity, CancellationToken cancellationToken);

    Task<int?> GetStockQuantityAsync(Guid productId, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, int>> GetStockQuantitiesAsync(IReadOnlyCollection<Guid> productIds, CancellationToken cancellationToken);
}
