using System.Text.Json;
using Common.SharedKernel.Abstractions.Events;
using Common.SharedKernel.Messaging;
using Common.SharedKernel.Observability.Context;
using Npgsql;
using Inventory.Api.Domain.Events;
using Inventory.Api.Features.Inventory.Events;

namespace Inventory.Api.Infrastructure.Outbox;

internal sealed class InventoryDomainEventDispatcher(IInventoryOutboxStore outboxStore) : IInventoryDomainEventDispatcher
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
            InventoryOutboxMessage outboxMessage = MapToOutboxMessage(domainEvent, appContext);
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

    private static InventoryOutboxMessage MapToOutboxMessage(IDomainEvent domainEvent, AppCallContextBase? appContext)
    {
        return domainEvent switch
        {
            InventoryInitializedDomainEvent initialized => CreateOutboxMessage(
                new InventoryInitializedIntegrationEvent(
                    initialized.ProductId,
                    initialized.StockQuantity,
                    initialized.CreatedAtUtc,
                    initialized.UpdatedAtUtc,
                    initialized.OccurredOnUtc),
                InventoryInitializedIntegrationEvent.Topic,
                initialized.ProductId,
                appContext),
            _ => throw new InvalidOperationException($"Unsupported domain event type '{domainEvent.GetType().Name}'.")
        };
    }

    private static InventoryOutboxMessage CreateOutboxMessage<T>(T integrationEvent, string topic, Guid aggregateId, AppCallContextBase? appContext)
        where T : Common.SharedKernel.Abstractions.IntegrationEvents.IIntegrationEvent
    {
        string aggregateKey = aggregateId.ToString("N");

        MessageMetadata metadata = new()
        {
            MessageId = integrationEvent.EventId.ToString("N"),
            Contract = new MessageContractDescriptor(integrationEvent.EventType, "1.0", "application/json"),
            CorrelationId = appContext?.CorrelationId,
            TraceId = appContext?.TraceId,
            SpanId = appContext?.SpanId,
            TenantId = appContext?.Headers.TryGetValue("X-Tenant-Id", out string? tenantId) == true ? tenantId : null,
            RoutingKey = aggregateKey,
            OrderingKey = aggregateKey
        };

        metadata.Headers["Source"] = "inventory-api";
        metadata.Headers["Entity"] = nameof(InventoryItem);
        metadata.Headers["EventType"] = integrationEvent.EventType;

        InventoryOutboxMetadata outboxMetadata = new()
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

        return new InventoryOutboxMessage
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
