namespace Products.Api.Features.Products.Create;

internal sealed record CreateProductCommand(
    string Name,
    string Description,
    string Sku,
    decimal Price,
    string Currency,
    string Category,
    string Brand,
    int StockQuantity,
    bool IsActive) : IRequest<Result<ProductResponse>>;
