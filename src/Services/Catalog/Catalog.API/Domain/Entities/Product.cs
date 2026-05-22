namespace Catalog.API.Domain.Entities;

public sealed class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ImageFile { get; set; } = string.Empty;

    public decimal Price { get; set; }
}