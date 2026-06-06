namespace Inventory.Api.Features.Inventory.GetBatch;

internal sealed record GetInventoryBatchQuery(IReadOnlyCollection<Guid> ProductIds) : IRequest<Result<InventoryBatchResponse>>;
