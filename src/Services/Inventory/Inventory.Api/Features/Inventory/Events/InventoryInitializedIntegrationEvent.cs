using System.Text.Json.Serialization;
using Common.SharedKernel.Abstractions.IntegrationEvents;

namespace Inventory.Api.Features.Inventory.Events;

internal sealed record InventoryInitializedIntegrationEvent : IntegrationEventBase
{
    public const string Topic = "inventory.events.v1";
    public const string EventTypeName = "inventory.initialized";

    public override string EventType => EventTypeName;

    [JsonConstructor]
    public InventoryInitializedIntegrationEvent(
        Guid eventId,
        DateTime occurredOnUtc,
        Guid productId,
        int stockQuantity,
        DateTime createdAtUtc,
        DateTime updatedAtUtc)
        : base(eventId, occurredOnUtc)
    {
        ProductId = productId;
        StockQuantity = stockQuantity;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public InventoryInitializedIntegrationEvent(
        Guid productId,
        int stockQuantity,
        DateTime createdAtUtc,
        DateTime updatedAtUtc,
        DateTime occurredOnUtc)
        : this(Guid.NewGuid(), occurredOnUtc, productId, stockQuantity, createdAtUtc, updatedAtUtc)
    {
    }

    public Guid ProductId { get; init; }

    public int StockQuantity { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }
}
