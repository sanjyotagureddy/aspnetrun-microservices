using Products.Api.Features.Products.Create;
using Products.Api.Features.Products.Events;
using Products.Api.Features.Products.Update;

namespace Products.Api.Infrastructure;

internal static class ProductMappings
{
    extension(CreateProductCommand request)
    {
        public Product ToDomain(Guid id, DateTime createdAtUtc)
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
    }

    extension(UpdateProductCommand request)
    {
        public Product ToDomain(Product existing, DateTime updatedAtUtc)
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
    }

    extension(Product product)
    {
        public ProductResponse ToResponse()
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

        public ProductCreatedIntegrationEvent ToCreatedIntegrationEvent(DateTime occurredOnUtc)
        {
            return new ProductCreatedIntegrationEvent(
                product.Id,
                product.Name,
                product.Sku,
                product.Price,
                product.Currency,
                product.Category,
                product.Brand,
                product.StockQuantity,
                product.IsActive,
                product.CreatedAt,
                occurredOnUtc);
        }
    }

    extension(CreateProductRequest request)
    {
        public CreateProductCommand ToCommand()
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
    }

    extension(UpdateProductRequest request)
    {
        public UpdateProductCommand ToCommand(Guid id)
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
}
