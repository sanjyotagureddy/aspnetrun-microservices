using ProductPolicyNamesAlias = global::Products.Api.Infrastructure.Security.ProductPolicyNames;

namespace Products.Api.Features.Products.Delete;

internal sealed class DeleteProductEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapProductsV1();

        group.MapDelete("/{id:guid}", HandleAsync)
            .WithName(ProductRouteNames.Delete)
            .RequireAuthorization(ProductPolicyNamesAlias.CatalogWritePolicy);
    }

    private static async Task<IResult> HandleAsync(IMediator mediator, Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteProductCommand(id), cancellationToken);
        return TypedResults.NoContent();
    }
}
