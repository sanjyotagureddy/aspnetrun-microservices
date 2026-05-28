namespace Products.Api.Features.Products.GetById;

internal sealed class GetProductByIdQueryHandler(IProductCatalogStore store)
    : IRequestHandler<GetProductByIdQuery, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        Product? product = await store.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            throw new Common.SharedKernel.Exceptions.NotFoundException(nameof(Product), request.Id.ToString());
        }

        return Result<ProductResponse>.Success(product.ToResponse());
    }
}
