namespace Products.Api.Features.Products.Get;

internal sealed record GetProductsQuery(
    string? Search,
    string? Category,
    string? Brand,
    bool? IsActive,
    int Page,
    int PageSize) : IRequest<Result<PagedResult<ProductResponse>>>;
