namespace Products.Api.Features.Products;

internal sealed class GetProductsEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapProductsV1();

        group.MapGet("/", HandleAsync)
            .WithName(ProductRouteNames.GetAll);
    }

    private static async Task<IResult> HandleAsync(
        IMediator mediator,
        string? search,
        string? category,
        string? brand,
        bool? isActive,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        GetProductsQuery query = new(search, category, brand, isActive, page, pageSize);
        Result<PagedResult<ProductResponse>> result = await mediator.Send(query, cancellationToken);
        return TypedResults.Ok(result.Value);
    }
}

internal sealed record GetProductsQuery(
    string? Search,
    string? Category,
    string? Brand,
    bool? IsActive,
    int Page,
    int PageSize) : IRequest<Result<PagedResult<ProductResponse>>>;

internal sealed class GetProductsQueryValidator : AbstractValidator<GetProductsQuery>
{
    public GetProductsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

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