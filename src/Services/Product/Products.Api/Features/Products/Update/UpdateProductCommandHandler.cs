using Common.SharedKernel.Messaging;
using Common.SharedKernel.Observability.Context;
using Products.Api.Features.Products.Events;
using Common.SharedKernel;

namespace Products.Api.Features.Products.Update;

internal sealed class UpdateProductCommandHandler(
    IProductCatalogStore store,
    IInventoryStockAdapter inventoryStockAdapter,
    TimeProvider timeProvider,
    Common.SharedKernel.Logging.ILogger<UpdateProductCommandHandler> logger,
    IMessageBus messageBus)
    : IRequestHandler<UpdateProductCommand, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        Product? product = await store.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
        {
            throw new Common.SharedKernel.Exceptions.NotFoundException(nameof(Product), request.Id.ToString());
        }

        DateTime occurredOnUtc = timeProvider.GetUtcNow().UtcDateTime;
        Product normalizedProduct = request.ToDomain(product, occurredOnUtc);
        await store.EnsureSkuIsUniqueAsync(normalizedProduct.Sku, normalizedProduct.Id, cancellationToken);
        await store.UpdateAsync(normalizedProduct, cancellationToken);

        int stockQuantity = await inventoryStockAdapter.GetStockQuantityAsync(normalizedProduct.Id, cancellationToken) ?? 0;
        ProductUpdatedIntegrationEvent productUpdated = normalizedProduct.ToUpdatedIntegrationEvent(occurredOnUtc, stockQuantity);
        AppCallContextBase? appContext = AppCallContextBase.Current;

        await messageBus.PublishAsync(
            ProductUpdatedIntegrationEvent.Topic,
            productUpdated,
            metadata =>
            {
                metadata.MessageId = productUpdated.EventId.ToString("N");
                metadata.OrderingKey = normalizedProduct.Id.ToString("N");
                metadata.Contract = new MessageContractDescriptor(nameof(ProductUpdatedIntegrationEvent), "1.0", "application/json");
                metadata.CorrelationId = appContext?.CorrelationId;
                metadata.TraceId = appContext?.TraceId;
                metadata.SpanId = appContext?.SpanId;
                metadata.TenantId = ResolveTenantId(appContext);
                metadata.Headers["Source"] = "products-api";
                metadata.Headers["Entity"] = nameof(Product);
                metadata.Headers["EventType"] = nameof(ProductUpdatedIntegrationEvent);
            },
            cancellationToken);

        await logger.LogInformationAsync(
            "Product updated",
            new Dictionary<string, object?>
            {
                ["productId"] = normalizedProduct.Id,
                ["sku"] = normalizedProduct.Sku,
                ["eventId"] = productUpdated.EventId,
                ["topic"] = ProductUpdatedIntegrationEvent.Topic
            },
            cancellationToken);

        return Result<ProductResponse>.Success(normalizedProduct.ToResponse(stockQuantity));
    }

    private static string? ResolveTenantId(AppCallContextBase? appContext)
    {
        if (appContext?.Headers is null)
        {
            return null;
        }

        return appContext.Headers.TryGetValue(Constants.Headers.TenantId, out string? tenantId)
            ? tenantId
            : null;
    }
}
