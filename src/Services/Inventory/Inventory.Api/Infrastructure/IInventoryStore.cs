namespace Inventory.Api.Infrastructure;

internal interface IInventoryStore
{
    Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken);

    Task InitializeAsync(InventoryItem item, CancellationToken cancellationToken);
}
