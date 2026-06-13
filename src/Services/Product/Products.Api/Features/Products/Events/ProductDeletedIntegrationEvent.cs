using System.Text.Json.Serialization;
using Common.SharedKernel.Abstractions.IntegrationEvents;

namespace Products.Api.Features.Products.Events;

[method: JsonConstructor]
internal sealed record ProductDeletedIntegrationEvent(
    Guid EventId,
    DateTime OccurredOnUtc,
    Guid ProductId,
    DateTime DeletedAtUtc)
    : IntegrationEventBase(EventId, OccurredOnUtc)
{
    public const string Topic = "products.events.v1";
    public const string EventTypeName = "product.deleted";

    public override string EventType => EventTypeName;

    public ProductDeletedIntegrationEvent(
        Guid productId,
        DateTime deletedAtUtc,
        DateTime occurredOnUtc)
        : this(Guid.NewGuid(), occurredOnUtc, productId, deletedAtUtc)
    {
    }
}
