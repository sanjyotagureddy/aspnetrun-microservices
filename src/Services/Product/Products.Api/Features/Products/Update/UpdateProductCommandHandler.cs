namespace Products.Api.Features.Products.Update;

using Common.SharedKernel.Logging;

internal sealed class UpdateProductCommandHandler(
    IProductCatalogStore store,
    IInventoryStockAdapter inventoryStockAdapter,
    TimeProvider timeProvider,
    ILogger<UpdateProductCommandHandler> logger)
    : IRequestHandler<UpdateProductCommand, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        Product? product = await store.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            throw new Common.SharedKernel.Exceptions.NotFoundException(nameof(Product), request.Id.ToString());
        }

        Product normalizedProduct = request.ToDomain(product, timeProvider.GetUtcNow().UtcDateTime);
        await store.EnsureSkuIsUniqueAsync(normalizedProduct.Sku, normalizedProduct.Id, cancellationToken);
        await store.UpdateAsync(normalizedProduct, cancellationToken);

        await logger.LogInformationAsync(
            "Product updated",
            new Dictionary<string, object?> { ["productId"] = normalizedProduct.Id, ["sku"] = normalizedProduct.Sku },
            cancellationToken);

        var stockQuantity = await inventoryStockAdapter.GetStockQuantityAsync(normalizedProduct.Id, cancellationToken) ?? 0;
        return Result<ProductResponse>.Success(normalizedProduct.ToResponse(stockQuantity));
    }
}
