namespace Inventory.Api.Features.Inventory.GetByProductId;

internal sealed class GetInventoryByProductIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapInventoryV1();

        group.MapGet("/{productId:guid}", HandleAsync)
            .WithName(InventoryRouteNames.GetByProductId);
    }

    private static async Task<IResult> HandleAsync(IMediator mediator, Guid productId, CancellationToken cancellationToken)
    {
        Result<InventoryResponse> result = await mediator.Send(new GetInventoryByProductIdQuery(productId), cancellationToken);
        return TypedResults.Ok(result.Value);
    }
}
