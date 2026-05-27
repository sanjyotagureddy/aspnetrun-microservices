using Products.Api.Features.Products.Create;
using Products.Api.Features.Products.Update;

namespace Products.Api.Infrastructure;

internal static class ProductMappings
{
    public static Product ToDomain(this CreateProductCommand request, Guid id, DateTime createdAtUtc)
    {
        return new Product(
            id,
            request.Name,
            request.Description,
            request.Sku,
            request.Price,
            request.Currency,
            request.Category,
            request.Brand,
            request.StockQuantity,
            request.IsActive,
            createdAtUtc);
    }

    public static Product ToDomain(this UpdateProductCommand request, Product existing, DateTime updatedAtUtc)
    {
        existing.Update(
            request.Name,
            request.Description,
            request.Sku,
            request.Price,
            request.Currency,
            request.Category,
            request.Brand,
            request.StockQuantity,
            request.IsActive,
            updatedAtUtc);

        return existing;
    }

    public static ProductResponse ToResponse(this Product product)
    {
        return new ProductResponse(
            product.Id,
            product.Name,
            product.Description,
            product.Sku,
            product.Price,
            product.Currency,
            product.Category,
            product.Brand,
            product.StockQuantity,
            product.IsActive,
            product.CreatedAt,
            product.UpdatedAt);
    }

    public static CreateProductCommand ToCommand(this CreateProductRequest request)
    {
        return new CreateProductCommand(
            request.Name,
            request.Description,
            request.Sku,
            request.Price,
            request.Currency,
            request.Category,
            request.Brand,
            request.StockQuantity,
            request.IsActive);
    }

    public static UpdateProductCommand ToCommand(this UpdateProductRequest request, Guid id)
    {
        return new UpdateProductCommand(
            id,
            request.Name,
            request.Description,
            request.Sku,
            request.Price,
            request.Currency,
            request.Category,
            request.Brand,
            request.StockQuantity,
            request.IsActive);
    }
}