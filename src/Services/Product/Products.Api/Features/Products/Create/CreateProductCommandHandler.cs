namespace Products.Api.Features.Products.Create;

using Common.SharedKernel.Logging;

internal sealed class CreateProductCommandHandler(IProductCatalogStore store, TimeProvider timeProvider, ILogger<CreateProductCommandHandler> logger)
    : IRequestHandler<CreateProductCommand, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        Product normalizedProduct = request.ToDomain(Guid.NewGuid(), timeProvider.GetUtcNow().UtcDateTime);
        await store.EnsureSkuIsUniqueAsync(normalizedProduct.Sku, null, cancellationToken);
        await store.AddAsync(normalizedProduct, cancellationToken);

        await logger.LogInformationAsync(
            "Product created",
            "product_created",
            new Dictionary<string, object?> { ["productId"] = normalizedProduct.Id, ["sku"] = normalizedProduct.Sku },
            cancellationToken);

        return Result<ProductResponse>.Success(normalizedProduct.ToResponse());
    }
}
