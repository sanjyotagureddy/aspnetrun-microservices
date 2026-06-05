namespace Products.Api.Features.Products;

internal static class ProductRouteGroupExtensions
{
    public static RouteGroupBuilder MapProductsV1(this IEndpointRouteBuilder app)
    {
        return app.MapGroup("/api/v1/products")
            .WithTags("Products");
    }
}