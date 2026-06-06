namespace Products.Api.Features.Products.Get;

internal sealed class GetProductsQueryHandler(IProductCatalogStore store, IInventoryStockAdapter inventoryStockAdapter)
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
        IReadOnlyDictionary<Guid, int> stockByProductId = await inventoryStockAdapter.GetStockQuantitiesAsync(
            result.Items.Select(item => item.Id).ToArray(),
            cancellationToken);

        PagedResult<ProductResponse> response = new(
            result.Items.Select(product => product.ToResponse(stockByProductId.GetValueOrDefault(product.Id, 0))).ToArray(),
            request.Page,
            request.PageSize,
            result.TotalCount);

        return Result<PagedResult<ProductResponse>>.Success(response);
    }
}
