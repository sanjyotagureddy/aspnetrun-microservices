namespace Products.Api.Features.Products.Update;

internal sealed class UpdateProductEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapProductsV1();

        group.MapPut("/{id:guid}", HandleAsync)
            .WithName(ProductRouteNames.Update);
    }

    private static async Task<IResult> HandleAsync(
        IMediator mediator,
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        Result<ProductResponse> result = await mediator.Send(request.ToCommand(id), cancellationToken);
        return TypedResults.Ok(result.Value);
    }
}
