namespace Products.Api.Features.Products.Get;

internal sealed class GetProductsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapProductsV1();

        group.MapGet("/", HandleAsync)
            .WithName(ProductRouteNames.GetAll);
    }

    private static async Task<IResult> HandleAsync(
        IMediator mediator,
        string? search,
        string? category,
        string? brand,
        bool? isActive,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        GetProductsQuery query = new(search, category, brand, isActive, page, pageSize);
        Result<PagedResult<ProductResponse>> result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result.Value);
    }
}
