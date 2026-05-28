namespace Products.Api.Infrastructure;

internal sealed record ProductSearchFilter(
    string? Search,
    string? Category,
    string? Brand,
    bool? IsActive,
    int Page,
    int PageSize);

internal sealed record ProductSearchResult(IReadOnlyCollection<Product> Items, int TotalCount);
