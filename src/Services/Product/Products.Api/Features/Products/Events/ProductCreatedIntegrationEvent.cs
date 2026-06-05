using System.Text.Json.Serialization;
using Common.SharedKernel.Abstractions.IntegrationEvents;

namespace Products.Api.Features.Products.Events;

internal sealed record ProductCreatedIntegrationEvent : IntegrationEventBase
{
    public const string Topic = "products.created";

    [JsonConstructor]
    public ProductCreatedIntegrationEvent(
        Guid eventId,
        DateTime occurredOnUtc,
        Guid productId,
        string name,
        string sku,
        decimal price,
        string currency,
        string category,
        string brand,
        int stockQuantity,
        bool isActive,
        DateTime createdAtUtc)
        : base(eventId, occurredOnUtc)
    {
        ProductId = productId;
        Name = name;
        Sku = sku;
        Price = price;
        Currency = currency;
        Category = category;
        Brand = brand;
        StockQuantity = stockQuantity;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
    }

    public ProductCreatedIntegrationEvent(
        Guid productId,
        string name,
        string sku,
        decimal price,
        string currency,
        string category,
        string brand,
        int stockQuantity,
        bool isActive,
        DateTime createdAtUtc,
        DateTime occurredOnUtc)
        : this(
            Guid.NewGuid(),
            occurredOnUtc,
            productId,
            name,
            sku,
            price,
            currency,
            category,
            brand,
            stockQuantity,
            isActive,
            createdAtUtc)
    {
    }

    public Guid ProductId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Sku { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public string Currency { get; init; } = string.Empty;

    public string Category { get; init; } = string.Empty;

    public string Brand { get; init; } = string.Empty;

    public int StockQuantity { get; init; }

    public bool IsActive { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}
