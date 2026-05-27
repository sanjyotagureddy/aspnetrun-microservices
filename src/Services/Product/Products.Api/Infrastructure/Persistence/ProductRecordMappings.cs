namespace Products.Api.Infrastructure.Persistence;

internal sealed class ProductRecord
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public string Currency { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Brand { get; set; } = string.Empty;

    public int StockQuantity { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public Product ToDomain()
    {
        var product = new Product(Id, Name, Description, Sku, Price, Currency, Category, Brand, StockQuantity, IsActive, CreatedAt);
        product.Update(Name, Description, Sku, Price, Currency, Category, Brand, StockQuantity, IsActive, UpdatedAt);
        return product;
    }
}

internal static class ProductRecordMappings
{
    public static object ToRecord(this Product product)
    {
        return new
        {
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
            product.UpdatedAt,
        };
    }
}
