using Common.SharedKernel.Exceptions;

namespace Products.Api.Features.Products;

internal sealed class GetProductByIdEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapProductsV1();

        group.MapGet("/{id:guid}", HandleAsync)
            .WithName(ProductRouteNames.GetById);
    }

    private static async Task<IResult> HandleAsync(IMediator mediator, Guid id, CancellationToken cancellationToken)
    {
        Result<ProductResponse> result = await mediator.Send(new GetProductByIdQuery(id), cancellationToken);
        return TypedResults.Ok(result.Value);
    }
}

internal sealed record GetProductByIdQuery(Guid Id) : IRequest<Result<ProductResponse>>;

internal sealed class GetProductByIdQueryValidator : AbstractValidator<GetProductByIdQuery>
{
    public GetProductByIdQueryValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

internal sealed class GetProductByIdQueryHandler(IProductCatalogStore store)
    : IRequestHandler<GetProductByIdQuery, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        Product? product = await store.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException(nameof(Product), request.Id.ToString());
        }

        return Result<ProductResponse>.Success(product.ToResponse());
    }
}