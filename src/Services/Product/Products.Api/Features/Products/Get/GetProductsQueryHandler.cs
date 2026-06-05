namespace Products.Api.Features.Products.Get;

internal sealed class GetProductsQueryHandler(IProductCatalogStore store)
    : IRequestHandler<GetProductsQuery, Result<PagedResult<ProductResponse>>>
{
    public async Task<Result<PagedResult<ProductResponse>>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        ProductSearchFilter filter = new(
            request.Search,
            request.Category,
            request.Brand,
            request.IsActive,
            request.Page,
            request.PageSize);

        ProductSearchResult result = await store.SearchAsync(filter, cancellationToken);
        PagedResult<ProductResponse> response = new(
            result.Items.Select(product => product.ToResponse()).ToArray(),
            request.Page,
            request.PageSize,
            result.TotalCount);

        return Result<PagedResult<ProductResponse>>.Success(response);
    }
}
