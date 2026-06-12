using Common.SharedKernel.Messaging;
using Common.SharedKernel.Messaging.Outbox;
using Common.SharedKernel.Logging;
using Products.Api.Features.Products.Events;

namespace Products.Api.Infrastructure.Outbox;

internal sealed class ProductOutboxPublisher(
    IProductOutboxStore outboxStore,
    IMessageBus messageBus,
    Common.SharedKernel.Logging.ILogger<ProductOutboxPublisher> logger)
    : OutboxPublisherBase<ProductOutboxMessage>(outboxStore, logger)
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
