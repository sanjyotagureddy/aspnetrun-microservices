namespace Inventory.Api.Features.Inventory.Initialize;

internal sealed class InitializeInventoryEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapInventoryV1();

        group.MapPut("/{productId:guid}/initialize", HandleAsync)
            .WithName(InventoryRouteNames.Initialize);
    }

    private static async Task<IResult> HandleAsync(
        IMediator mediator,
        Guid productId,
        InitializeInventoryRequest request,
        CancellationToken cancellationToken)
    {
        Result result = await mediator.Send(request.ToCommand(productId), cancellationToken);
        return result.IsSuccess ? TypedResults.NoContent() : TypedResults.BadRequest();
    }
}
