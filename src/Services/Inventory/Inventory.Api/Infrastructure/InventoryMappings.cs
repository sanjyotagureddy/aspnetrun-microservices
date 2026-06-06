using Inventory.Api.Features.Inventory.Initialize;

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
}
