namespace Products.Api.Features.Products.Update;

internal sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    string Sku,
    decimal Price,
    string Currency,
    string Category,
    string Brand,
    int StockQuantity,
    bool IsActive) : IRequest<Result<ProductResponse>>;
