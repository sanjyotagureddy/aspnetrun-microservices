namespace Products.Api.Features.Products.Create;

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
