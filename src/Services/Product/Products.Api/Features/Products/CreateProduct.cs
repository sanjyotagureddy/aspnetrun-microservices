namespace Products.Api.Features.Products;

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

internal sealed record CreateProductCommand(
    string Name,
    string Description,
    string Sku,
    decimal Price,
    string Currency,
    string Category,
    string Brand,
    int StockQuantity,
    bool IsActive) : IRequest<Result<ProductResponse>>;

internal sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
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

internal sealed class CreateProductCommandHandler(IProductCatalogStore store, TimeProvider timeProvider)
    : IRequestHandler<CreateProductCommand, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        Product normalizedProduct = request.ToDomain(Guid.NewGuid(), timeProvider.GetUtcNow().UtcDateTime);
        await store.EnsureSkuIsUniqueAsync(normalizedProduct.Sku, null, cancellationToken);
        await store.AddAsync(normalizedProduct, cancellationToken);
        return Result<ProductResponse>.Success(normalizedProduct.ToResponse());
    }
}
