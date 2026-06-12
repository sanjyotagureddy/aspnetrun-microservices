using Common.SharedKernel.Messaging;
using Common.SharedKernel.Messaging.Outbox;
using Common.SharedKernel.Logging;
using Inventory.Api.Features.Inventory.Events;

namespace Inventory.Api.Infrastructure.Outbox;

internal sealed class InventoryOutboxPublisher(
    IInventoryOutboxStore outboxStore,
    IMessageBus messageBus,
    Common.SharedKernel.Logging.ILogger<InventoryOutboxPublisher> logger)
    : OutboxPublisherBase<InventoryOutboxMessage>(outboxStore, logger)
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
