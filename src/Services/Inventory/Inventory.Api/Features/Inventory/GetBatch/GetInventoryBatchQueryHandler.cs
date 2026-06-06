namespace Inventory.Api.Features.Inventory.GetBatch;

internal sealed class GetInventoryBatchQueryHandler(IInventoryStore store)
    : IRequestHandler<GetInventoryBatchQuery, Result<InventoryBatchResponse>>
{
    public async Task<Result<InventoryBatchResponse>> Handle(GetInventoryBatchQuery request, CancellationToken cancellationToken)
    {
        Guid[] distinctProductIds = request.ProductIds.Distinct().ToArray();
        IReadOnlyDictionary<Guid, int> stockByProductId = await store.GetStockByProductIdsAsync(distinctProductIds, cancellationToken);

        Dictionary<Guid, int> normalizedStockByProductId = distinctProductIds.ToDictionary(
            productId => productId,
            productId => stockByProductId.GetValueOrDefault(productId, 0));

        return Result<InventoryBatchResponse>.Success(new InventoryBatchResponse(normalizedStockByProductId));
    }
}
