namespace Products.Api.Features.Products.Update;

internal sealed class UpdateProductCommandHandler(IProductCatalogStore store, TimeProvider timeProvider)
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
        return Result<ProductResponse>.Success(normalizedProduct.ToResponse());
    }
}
