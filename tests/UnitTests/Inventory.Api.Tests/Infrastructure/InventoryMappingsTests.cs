using Inventory.Api.Contracts;
using Inventory.Api.Features.Inventory.GetBatch;
using Inventory.Api.Features.Inventory.Initialize;
using Inventory.Api.Infrastructure;

namespace Inventory.Api.Tests.Infrastructure;

public sealed class InventoryMappingsTests
{
    [Fact]
    public void ToCommand_ShouldMapInitializeRequest()
    {
        InitializeInventoryRequest request = new(12);
        Guid productId = Guid.NewGuid();

        InitializeInventoryCommand command = request.ToCommand(productId);

        Assert.Equal(productId, command.ProductId);
        Assert.Equal(12, command.StockQuantity);
    }

    [Fact]
    public void ToQuery_ShouldMapBatchRequest()
    {
        Guid[] productIds = [Guid.NewGuid(), Guid.NewGuid()];
        InventoryBatchRequest request = new(productIds);

        GetInventoryBatchQuery query = request.ToQuery();

        Assert.Equal(productIds, query.ProductIds);
    }
}
