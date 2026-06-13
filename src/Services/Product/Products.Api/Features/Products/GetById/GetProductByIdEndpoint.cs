using ProductPolicyNamesAlias = global::Products.Api.Infrastructure.Security.ProductPolicyNames;

namespace Products.Api.Features.Products.GetById;

internal sealed class GetProductByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapProductsV1();

        group.MapGet("/{id:guid}", HandleAsync)
            .WithName(ProductRouteNames.GetById)
            .RequireAuthorization(ProductPolicyNamesAlias.TenantReadPolicy);
    }

    private static async Task<IResult> HandleAsync(IMediator mediator, Guid id, CancellationToken cancellationToken)
    {
        Result<ProductResponse> result = await mediator.Send(new GetProductByIdQuery(id), cancellationToken);
        return TypedResults.Ok(result.Value);
    }
}
