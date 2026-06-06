namespace Inventory.Api.Features.Inventory.Initialize;

internal sealed record InitializeInventoryCommand(Guid ProductId, int StockQuantity) : IRequest<Result>;
