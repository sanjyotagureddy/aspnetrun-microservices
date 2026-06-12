using System.Text.Json.Serialization;
using Common.SharedKernel.Abstractions.IntegrationEvents;

namespace Products.Api.Features.Products.Events;

internal sealed record ProductUpdatedIntegrationEvent : IntegrationEventBase
{
    public const string Topic = "products.events.v1";
    public const string EventTypeName = "product.updated";

    public override string EventType => EventTypeName;

    [JsonConstructor]
    public ProductUpdatedIntegrationEvent(
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
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
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
        UpdatedAtUtc = updatedAtUtc;
    }

    public ProductUpdatedIntegrationEvent(
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
        DateTime updatedAtUtc,
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
            createdAtUtc,
            updatedAtUtc)
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

    public DateTime UpdatedAtUtc { get; init; }
}
