namespace Inventory.Api.Features.Inventory.GetByProductId;

internal sealed record GetInventoryByProductIdQuery(Guid ProductId) : IRequest<Result<InventoryResponse>>;
