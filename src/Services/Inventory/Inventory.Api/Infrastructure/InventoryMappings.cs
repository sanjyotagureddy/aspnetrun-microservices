using Inventory.Api.Features.Inventory.Initialize;
using Inventory.Api.Features.Inventory.GetBatch;

namespace Inventory.Api.Infrastructure;

internal static class InventoryMappings
{
    extension(InitializeInventoryRequest request)
    {
        public InitializeInventoryCommand ToCommand(Guid productId)
        {
            return new InitializeInventoryCommand(productId, request.StockQuantity);
        }
    }

    extension(InventoryBatchRequest request)
    {
        public GetInventoryBatchQuery ToQuery()
        {
            return new GetInventoryBatchQuery(request.ProductIds);
        }
    }
}
