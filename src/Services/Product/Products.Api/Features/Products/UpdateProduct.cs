using Common.SharedKernel.Exceptions;

namespace Products.Api.Features.Products;

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

internal sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    string Sku,
    decimal Price,
    string Currency,
    string Category,
    string Brand,
    int StockQuantity,
    bool IsActive) : IRequest<Result<ProductResponse>>;

internal sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2_000);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Brand).NotEmpty().MaximumLength(200);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
    }
}

internal sealed class UpdateProductCommandHandler(IProductCatalogStore store, TimeProvider timeProvider)
    : IRequestHandler<UpdateProductCommand, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        Product? product = await store.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            throw new NotFoundException(nameof(Product), request.Id.ToString());
        }

        Product normalizedProduct = request.ToDomain(product, timeProvider.GetUtcNow().UtcDateTime);
        await store.EnsureSkuIsUniqueAsync(normalizedProduct.Sku, normalizedProduct.Id, cancellationToken);
        await store.UpdateAsync(normalizedProduct, cancellationToken);
        return Result<ProductResponse>.Success(normalizedProduct.ToResponse());
    }
}