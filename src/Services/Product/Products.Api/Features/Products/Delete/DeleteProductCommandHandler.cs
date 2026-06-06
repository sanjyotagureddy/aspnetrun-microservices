using Common.SharedKernel.Messaging;
using Common.SharedKernel.Observability.Context;
using Products.Api.Features.Products.Events;

namespace Products.Api.Features.Products.Delete;

internal sealed class DeleteProductCommandHandler(
    IProductCatalogStore store,
    TimeProvider timeProvider,
    Common.SharedKernel.Logging.ILogger<DeleteProductCommandHandler> logger,
    IMessageBus messageBus)
    : IRequestHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        bool deleted = await store.DeleteAsync(request.Id, cancellationToken);
        if (!deleted)
        {
            throw new Common.SharedKernel.Exceptions.NotFoundException(nameof(Product), request.Id.ToString());
        }

        DateTime occurredOnUtc = timeProvider.GetUtcNow().UtcDateTime;
        ProductDeletedIntegrationEvent productDeleted = new(request.Id, occurredOnUtc, occurredOnUtc);
        AppCallContextBase? appContext = AppCallContextBase.Current;

        await messageBus.PublishAsync(
            ProductDeletedIntegrationEvent.Topic,
            productDeleted,
            metadata =>
            {
                metadata.MessageId = productDeleted.EventId.ToString("N");
                metadata.OrderingKey = request.Id.ToString("N");
                metadata.Contract = new MessageContractDescriptor(nameof(ProductDeletedIntegrationEvent), "1.0", "application/json");
                metadata.CorrelationId = appContext?.CorrelationId;
                metadata.TraceId = appContext?.TraceId;
                metadata.SpanId = appContext?.SpanId;
                metadata.TenantId = appContext?.Headers.TryGetValue("X-Tenant-Id", out string? tenantId) == true ? tenantId : null;
                metadata.Headers["Source"] = "Products.Api";
                metadata.Headers["Entity"] = nameof(Product);
                metadata.Headers["EventType"] = nameof(ProductDeletedIntegrationEvent);
            },
            cancellationToken);

        await logger.LogInformationAsync(
            "Product deleted",
            new Dictionary<string, object?>
            {
                ["productId"] = request.Id,
                ["eventId"] = productDeleted.EventId,
                ["topic"] = ProductDeletedIntegrationEvent.Topic
            },
            cancellationToken);

        return Result.Success();
    }
}
