namespace Inventory.Api.Contracts;

public sealed record InventoryResponse(Guid ProductId, int StockQuantity);

public sealed record InitializeInventoryRequest(int StockQuantity);
