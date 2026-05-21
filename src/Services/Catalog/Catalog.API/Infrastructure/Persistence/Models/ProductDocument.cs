using Catalog.API.Domain.Entities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Catalog.API.Infrastructure.Persistence.Models;

public sealed class ProductDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.String)]
    public Guid Id { get; set; } = Guid.NewGuid();

    [BsonElement("Name")]
    public string Name { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string Summary { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public string ImageFile { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public static ProductDocument FromDomain(Product product)
    {
        return new ProductDocument
        {
            Id = product.Id,
            Name = product.Name,
            Category = product.Category,
            Summary = product.Summary,
            Description = product.Description,
            ImageFile = product.ImageFile,
            Price = product.Price
        };
    }

    public Product ToDomain()
    {
        return new Product
        {
            Id = Id,
            Name = Name,
            Category = Category,
            Summary = Summary,
            Description = Description,
            ImageFile = ImageFile,
            Price = Price
        };
    }
}