namespace Inventory.Api.Features.Inventory.GetBatch;

internal sealed class GetInventoryBatchEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapInventoryV1();

        group.MapPost("/batch", HandleAsync)
            .WithName(InventoryRouteNames.GetBatch);
    }

    private static async Task<IResult> HandleAsync(
        IMediator mediator,
        InventoryBatchRequest request,
        CancellationToken cancellationToken)
    {
        Result<InventoryBatchResponse> result = await mediator.Send(request.ToQuery(), cancellationToken);
        return TypedResults.Ok(result.Value);
    }
}
