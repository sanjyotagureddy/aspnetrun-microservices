using Common.SharedKernel.Messaging;
using Common.SharedKernel.Messaging.Outbox;
using Inventory.Api.Features.Inventory.Events;

namespace Inventory.Api.Infrastructure.Outbox;

internal sealed class InventoryOutboxPublisher(
    IInventoryOutboxStore outboxStore,
    IMessageBus messageBus,
    ILogger<InventoryOutboxPublisher> logger)
    : OutboxPublisherBase<InventoryOutboxMessage>(outboxStore, logger)
{
    protected override string FailureLogTemplate => "Failed to publish inventory outbox message {OutboxId} for event type {EventType}.";

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
