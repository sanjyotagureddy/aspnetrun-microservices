namespace Products.Api.Features.Products.Create;

internal sealed class CreateProductEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapProductsV1();

        group.MapPost("/", HandleAsync)
            .WithName(ProductRouteNames.Create);
    }

    private static async Task<IResult> HandleAsync(
        IMediator mediator,
        CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        Result<ProductResponse> result = await mediator.Send(request.ToCommand(), cancellationToken);
        return TypedResults.CreatedAtRoute(result.Value, ProductRouteNames.GetById, new { id = result.Value!.Id });
    }
}
