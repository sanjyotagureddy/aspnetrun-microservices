using Common.SharedKernel.Messaging;
using Common.SharedKernel.Messaging.Outbox;
using Inventory.Api.Features.Inventory.Events;
using Microsoft.Extensions.Options;

namespace Inventory.Api.Infrastructure.Outbox;

internal sealed class InventoryOutboxPublisher(
    IInventoryOutboxStore outboxStore,
    IMessageBus messageBus,
    Common.SharedKernel.Logging.ILogger<InventoryOutboxPublisher> logger,
    IOptions<MessagingOptions> messagingOptions)
    : OutboxPublisherBase<InventoryOutboxMessage>(
        outboxStore,
        logger,
        backlogHeartbeatEnabled: messagingOptions.Value.OutboxHeartbeat.Enabled,
        backlogHeartbeatInterval: messagingOptions.Value.OutboxHeartbeat.Interval)
{
    protected override async Task PublishAsync(InventoryOutboxMessage outboxMessage, CancellationToken cancellationToken)
    {
        InventoryOutboxMetadata metadata = OutboxPublisherHelpers.DeserializeMetadata<InventoryOutboxMetadata>(outboxMessage.MetadataJson);

        switch (outboxMessage.EventType)
        {
            case InventoryInitializedIntegrationEvent.EventTypeName:
            {
                InventoryInitializedIntegrationEvent payload = OutboxPublisherHelpers.DeserializePayload<InventoryInitializedIntegrationEvent>(outboxMessage.PayloadJson);
                await messageBus.PublishAsync(outboxMessage.Topic, payload, m => OutboxPublisherHelpers.CopyMetadata(metadata, m), cancellationToken);
                return;
            }
            default:
                throw new InvalidOperationException($"Unsupported inventory outbox event type '{outboxMessage.EventType}'.");
        }
    }
}
