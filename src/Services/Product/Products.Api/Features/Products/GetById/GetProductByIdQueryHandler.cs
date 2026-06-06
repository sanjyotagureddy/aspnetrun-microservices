namespace Products.Api.Features.Products.GetById;

internal sealed class GetProductByIdQueryHandler(IProductCatalogStore store, IInventoryStockAdapter inventoryStockAdapter)
    : IRequestHandler<GetProductByIdQuery, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        Product? product = await store.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            throw new Common.SharedKernel.Exceptions.NotFoundException(nameof(Product), request.Id.ToString());
        }

        int stockQuantity = await inventoryStockAdapter.GetStockQuantityAsync(product.Id, cancellationToken) ?? 0;
        return Result<ProductResponse>.Success(product.ToResponse(stockQuantity));
    }
}
