using System.Text.Json;
using Common.SharedKernel.Abstractions.Events;
using Common.SharedKernel.Messaging;
using Common.SharedKernel.Observability.Context;
using Npgsql;
using Products.Api.Domain.Events;
using Products.Api.Features.Products.Events;

namespace Products.Api.Infrastructure.Outbox;

internal sealed class ProductDomainEventDispatcher(IProductOutboxStore outboxStore) : IProductDomainEventDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task DispatchAsync(
        IEnumerable<IDomainEvent> domainEvents,
        AppCallContextBase? appContext,
        NpgsqlConnection? connection,
        NpgsqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        foreach (IDomainEvent domainEvent in domainEvents)
        {
            ProductOutboxMessage outboxMessage = MapToOutboxMessage(domainEvent, appContext);
            if (connection is not null && transaction is not null)
            {
                await outboxStore.EnqueueAsync(outboxMessage, connection, transaction, cancellationToken);
            }
            else
            {
                await outboxStore.EnqueueAsync(outboxMessage, cancellationToken);
            }
        }
    }

    private static ProductOutboxMessage MapToOutboxMessage(IDomainEvent domainEvent, AppCallContextBase? appContext)
    {
        return domainEvent switch
        {
            ProductCreatedDomainEvent created => CreateOutboxMessage(
                new ProductCreatedIntegrationEvent(
                    created.ProductId,
                    created.Name,
                    created.Sku,
                    created.Price,
                    created.Currency,
                    created.Category,
                    created.Brand,
                    created.StockQuantity,
                    created.IsActive,
                    created.CreatedAtUtc,
                    created.OccurredOnUtc),
                ProductCreatedIntegrationEvent.Topic,
                appContext),

            ProductUpdatedDomainEvent updated => CreateOutboxMessage(
                new ProductUpdatedIntegrationEvent(
                    updated.ProductId,
                    updated.Name,
                    updated.Sku,
                    updated.Price,
                    updated.Currency,
                    updated.Category,
                    updated.Brand,
                    updated.StockQuantity,
                    updated.IsActive,
                    updated.CreatedAtUtc,
                    updated.UpdatedAtUtc,
                    updated.OccurredOnUtc),
                ProductUpdatedIntegrationEvent.Topic,
                appContext),

            ProductDeletedDomainEvent deleted => CreateOutboxMessage(
                new ProductDeletedIntegrationEvent(
                    deleted.ProductId,
                    deleted.DeletedAtUtc,
                    deleted.OccurredOnUtc),
                ProductDeletedIntegrationEvent.Topic,
                appContext),

            _ => throw new InvalidOperationException($"Unsupported domain event type '{domainEvent.GetType().Name}'.")
        };
    }

    private static ProductOutboxMessage CreateOutboxMessage<T>(T integrationEvent, string topic, AppCallContextBase? appContext)
        where T : Common.SharedKernel.Abstractions.IntegrationEvents.IIntegrationEvent
    {
        MessageMetadata metadata = new()
        {
            MessageId = integrationEvent.EventId.ToString("N"),
            Contract = new MessageContractDescriptor(integrationEvent.EventType, "1.0", "application/json"),
            CorrelationId = appContext?.CorrelationId,
            TraceId = appContext?.TraceId,
            SpanId = appContext?.SpanId,
            TenantId = appContext?.Headers.TryGetValue("X-Tenant-Id", out string? tenantId) == true ? tenantId : null
        };

        metadata.Headers["Source"] = "products-api";
        metadata.Headers["Entity"] = nameof(Product);
        metadata.Headers["EventType"] = integrationEvent.EventType;

        ProductOutboxMetadata outboxMetadata = new()
        {
            MessageId = metadata.MessageId,
            CorrelationId = metadata.CorrelationId,
            CausationId = metadata.CausationId,
            TraceId = metadata.TraceId,
            SpanId = metadata.SpanId,
            TenantId = metadata.TenantId,
            RoutingKey = metadata.RoutingKey,
            OrderingKey = metadata.OrderingKey,
            Contract = metadata.Contract,
            Headers = metadata.Headers.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase),
            TransportHints = metadata.TransportHints.ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.OrdinalIgnoreCase)
        };

        return new ProductOutboxMessage
        {
            Id = Guid.NewGuid(),
            OccurredOnUtc = integrationEvent.OccurredOnUtc,
            EventType = integrationEvent.EventType,
            Topic = topic,
            PayloadJson = JsonSerializer.Serialize(integrationEvent, JsonOptions),
            MetadataJson = JsonSerializer.Serialize(outboxMetadata, JsonOptions)
        };
    }
}
