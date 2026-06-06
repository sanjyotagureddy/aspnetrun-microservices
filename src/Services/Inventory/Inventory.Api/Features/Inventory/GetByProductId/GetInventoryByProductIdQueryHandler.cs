using Inventory.Api.Infrastructure.Persistence;

namespace Inventory.Api.Features.Inventory.GetByProductId;

internal sealed class GetInventoryByProductIdQueryHandler(IInventoryStore store)
    : IRequestHandler<GetInventoryByProductIdQuery, Result<InventoryResponse>>
{
    public async Task<Result<InventoryResponse>> Handle(GetInventoryByProductIdQuery request, CancellationToken cancellationToken)
    {
        InventoryItem? item = await store.GetByProductIdAsync(request.ProductId, cancellationToken);
        InventoryResponse response = item is null
            ? new InventoryResponse(request.ProductId, 0)
            : item.ToResponse();

        return Result<InventoryResponse>.Success(response);
    }
}
