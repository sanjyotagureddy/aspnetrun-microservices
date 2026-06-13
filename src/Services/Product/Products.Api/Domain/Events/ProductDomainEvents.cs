using Common.SharedKernel.Abstractions.Events;

namespace Products.Api.Domain.Events;

internal sealed record ProductCreatedDomainEvent(
    DateTime OccurredAtUtc,
    Guid ProductId,
    string Name,
    string Sku,
    decimal Price,
    string Currency,
    string Category,
    string Brand,
    int StockQuantity,
    bool IsActive,
    DateTime CreatedAtUtc) : DomainEvent(OccurredAtUtc)
{
    public const string EventTypeName = "product.created";

    public override string EventType => EventTypeName;
}

internal sealed record ProductUpdatedDomainEvent(
    DateTime OccurredAtUtc,
    Guid ProductId,
    string Name,
    string Sku,
    decimal Price,
    string Currency,
    string Category,
    string Brand,
    int StockQuantity,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc) : DomainEvent(OccurredAtUtc)
{
    public const string EventTypeName = "product.updated";

    public override string EventType => EventTypeName;
}

internal sealed record ProductDeletedDomainEvent(
    DateTime OccurredAtUtc,
    Guid ProductId,
    DateTime DeletedAtUtc) : DomainEvent(OccurredAtUtc)
{
    public const string EventTypeName = "product.deleted";

    public override string EventType => EventTypeName;
}
