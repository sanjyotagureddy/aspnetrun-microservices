using System.Text.Json;
using Common.SharedKernel.Messaging;
using Inventory.Api.Features.Inventory.Events;

namespace Inventory.Api.Infrastructure.Outbox;

internal sealed class InventoryOutboxPublisher(
    IInventoryOutboxStore outboxStore,
    IMessageBus messageBus,
    ILogger<InventoryOutboxPublisher> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IReadOnlyList<InventoryOutboxMessage> messages = await outboxStore.GetPendingAsync(50, stoppingToken);
            if (messages.Count == 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                continue;
            }

            foreach (InventoryOutboxMessage message in messages)
            {
                try
                {
                    await PublishAsync(message, stoppingToken);
                    await outboxStore.MarkPublishedAsync(message.Id, stoppingToken);
                }
                catch (Exception ex)
                {
                    await outboxStore.MarkFailedAsync(message.Id, message.AttemptCount + 1, ex.Message, stoppingToken);
                    logger.LogWarning(ex, "Failed to publish inventory outbox message {OutboxId} for event type {EventType}.", message.Id, message.EventType);
                }
            }
        }
    }

    private async Task PublishAsync(InventoryOutboxMessage outboxMessage, CancellationToken cancellationToken)
    {
        InventoryOutboxMetadata metadata = DeserializeMetadata(outboxMessage.MetadataJson);

        switch (outboxMessage.EventType)
        {
            case InventoryInitializedIntegrationEvent.EventTypeName:
            {
                InventoryInitializedIntegrationEvent payload = DeserializePayload<InventoryInitializedIntegrationEvent>(outboxMessage.PayloadJson);
                await messageBus.PublishAsync(outboxMessage.Topic, payload, m => CopyMetadata(metadata, m), cancellationToken);
                return;
            }
            default:
                throw new InvalidOperationException($"Unsupported inventory outbox event type '{outboxMessage.EventType}'.");
        }
    }

    private static T DeserializePayload<T>(string payloadJson)
    {
        T? payload = JsonSerializer.Deserialize<T>(payloadJson, JsonOptions);
        return payload ?? throw new InvalidOperationException($"Unable to deserialize payload for '{typeof(T).Name}'.");
    }

    private static InventoryOutboxMetadata DeserializeMetadata(string metadataJson)
    {
        InventoryOutboxMetadata? metadata = JsonSerializer.Deserialize<InventoryOutboxMetadata>(metadataJson, JsonOptions);
        return metadata ?? new InventoryOutboxMetadata();
    }

    private static void CopyMetadata(InventoryOutboxMetadata source, MessageMetadata destination)
    {
        destination.MessageId = source.MessageId;
        destination.CorrelationId = source.CorrelationId;
        destination.CausationId = source.CausationId;
        destination.TraceId = source.TraceId;
        destination.SpanId = source.SpanId;
        destination.TenantId = source.TenantId;
        destination.RoutingKey = source.RoutingKey;
        destination.OrderingKey = source.OrderingKey;
        destination.Contract = new MessageContractDescriptor(
            source.Contract.MessageType,
            source.Contract.Version,
            source.Contract.ContentType,
            source.Contract.SchemaRef,
            source.Contract.Compatibility);

        foreach (KeyValuePair<string, string> header in source.Headers)
        {
            destination.Headers[header.Key] = header.Value;
        }

        foreach (KeyValuePair<string, string> hint in source.TransportHints)
        {
            destination.TransportHints[hint.Key] = hint.Value;
        }
    }
}
