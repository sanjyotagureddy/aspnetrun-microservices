using Common.SharedKernel.Messaging;
using Common.SharedKernel.Messaging.Outbox;
using Microsoft.Extensions.Options;
using Products.Api.Features.Products.Events;

namespace Products.Api.Infrastructure.Outbox;

internal sealed class ProductOutboxPublisher(
    IProductOutboxStore outboxStore,
    IMessageBus messageBus,
    Common.SharedKernel.Logging.ILogger<ProductOutboxPublisher> logger,
    IOptions<MessagingOptions> messagingOptions)
    : OutboxPublisherBase<ProductOutboxMessage>(
        outboxStore,
        logger,
        backlogHeartbeatEnabled: messagingOptions.Value.OutboxHeartbeat.Enabled,
        backlogHeartbeatInterval: messagingOptions.Value.OutboxHeartbeat.Interval)
{
    protected override async Task PublishAsync(ProductOutboxMessage outboxMessage, CancellationToken cancellationToken)
    {
        ProductOutboxMetadata metadata = OutboxPublisherHelpers.DeserializeMetadata<ProductOutboxMetadata>(outboxMessage.MetadataJson);

        switch (outboxMessage.EventType)
        {
            case ProductCreatedIntegrationEvent.EventTypeName:
            {
                ProductCreatedIntegrationEvent payload = OutboxPublisherHelpers.DeserializePayload<ProductCreatedIntegrationEvent>(outboxMessage.PayloadJson);
                await messageBus.PublishAsync(outboxMessage.Topic, payload, m => OutboxPublisherHelpers.CopyMetadata(metadata, m), cancellationToken);
                return;
            }
            case ProductUpdatedIntegrationEvent.EventTypeName:
            {
                ProductUpdatedIntegrationEvent payload = OutboxPublisherHelpers.DeserializePayload<ProductUpdatedIntegrationEvent>(outboxMessage.PayloadJson);
                await messageBus.PublishAsync(outboxMessage.Topic, payload, m => OutboxPublisherHelpers.CopyMetadata(metadata, m), cancellationToken);
                return;
            }
            case ProductDeletedIntegrationEvent.EventTypeName:
            {
                ProductDeletedIntegrationEvent payload = OutboxPublisherHelpers.DeserializePayload<ProductDeletedIntegrationEvent>(outboxMessage.PayloadJson);
                await messageBus.PublishAsync(outboxMessage.Topic, payload, m => OutboxPublisherHelpers.CopyMetadata(metadata, m), cancellationToken);
                return;
            }
            default:
                throw new InvalidOperationException($"Unsupported outbox event type '{outboxMessage.EventType}'.");
        }
    }
}
