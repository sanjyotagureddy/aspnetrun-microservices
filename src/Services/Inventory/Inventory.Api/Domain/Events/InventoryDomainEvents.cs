using Common.SharedKernel.Abstractions.Events;

namespace Inventory.Api.Domain.Events;

internal sealed record InventoryInitializedDomainEvent(
    DateTime OccurredAtUtc,
    Guid ProductId,
    int StockQuantity,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc) : DomainEvent(OccurredAtUtc)
{
    public const string EventTypeName = "inventory.initialized";

    public override string EventType => EventTypeName;
}
