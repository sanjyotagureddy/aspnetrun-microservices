namespace Inventory.Api.Contracts;

public sealed record InventoryResponse(Guid ProductId, int StockQuantity);

public sealed record InitializeInventoryRequest(int StockQuantity);

public sealed record InventoryBatchRequest(IReadOnlyCollection<Guid> ProductIds);

public sealed record InventoryBatchResponse(IReadOnlyDictionary<Guid, int> StockByProductId);
