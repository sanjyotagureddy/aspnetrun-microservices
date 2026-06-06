using Inventory.Api.Features.Inventory;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Inventory.Api.Tests.Features.Inventory;

public sealed class InventoryRouteTests
{
    [Fact]
    public void RouteNames_ShouldMatchExpectedValues()
    {
        Assert.Equal("Inventory_GetByProductId", InventoryRouteNames.GetByProductId);
        Assert.Equal("Inventory_GetBatch", InventoryRouteNames.GetBatch);
        Assert.Equal("Inventory_Initialize", InventoryRouteNames.Initialize);
    }

    [Fact]
    public void MapInventoryV1_ShouldCreateRouteGroup()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        WebApplication app = builder.Build();

        RouteGroupBuilder group = app.MapInventoryV1();

        Assert.NotNull(group);
    }
}
