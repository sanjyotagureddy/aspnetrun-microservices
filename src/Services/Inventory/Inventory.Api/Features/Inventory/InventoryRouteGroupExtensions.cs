namespace Inventory.Api.Features.Inventory;

internal static class InventoryRouteGroupExtensions
{
    public static RouteGroupBuilder MapInventoryV1(this IEndpointRouteBuilder app)
    {
        return app.MapGroup("/api/v1/inventory")
            .WithTags("Inventory");
    }
}
