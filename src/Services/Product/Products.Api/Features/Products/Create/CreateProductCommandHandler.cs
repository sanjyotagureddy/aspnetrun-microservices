using Common.SharedKernel.Messaging;
using Common.SharedKernel.Observability.Context;
using Products.Api.Features.Products.Events;
using Products.Api.Observability;

namespace Products.Api.Features.Products.Create;

internal sealed class CreateProductCommandHandler(
    IProductCatalogStore store,
    IInventoryStockAdapter inventoryStockAdapter,
    TimeProvider timeProvider,
    Common.SharedKernel.Logging.ILogger<CreateProductCommandHandler> logger,
    IMessageBus messageBus)
    : IRequestHandler<CreateProductCommand, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        DateTime occurredOnUtc = timeProvider.GetUtcNow().UtcDateTime;
        Product normalizedProduct = request.ToDomain(Guid.NewGuid(), occurredOnUtc);

        await store.EnsureSkuIsUniqueAsync(normalizedProduct.Sku, null, cancellationToken);
        await store.AddAsync(normalizedProduct, cancellationToken);

        try
        {
            await inventoryStockAdapter.InitializeAsync(normalizedProduct.Id, request.StockQuantity, cancellationToken);
        }
        catch (Exception ex)
        {
            await logger.LogWarningAsync(
                "Inventory initialization failed for product",
                "inventory_initialize_failed",
                new Dictionary<string, object?>
                {
                    ["productId"] = normalizedProduct.Id,
                    ["stockQuantity"] = request.StockQuantity,
                    ["exceptionType"] = ex.GetType().Name
                },
                cancellationToken);
        }

        ProductCreatedIntegrationEvent productCreated = normalizedProduct.ToCreatedIntegrationEvent(occurredOnUtc, request.StockQuantity);
        AppCallContext? appContext = AppCallContextBase.CurrentAs<AppCallContext>();

        await messageBus.PublishAsync(
            ProductCreatedIntegrationEvent.Topic,
            productCreated,
            metadata =>
            {
                metadata.MessageId = productCreated.EventId.ToString("N");
                metadata.Key = normalizedProduct.Id.ToString("N");
                metadata.CorrelationId = appContext?.CorrelationId;
                metadata.TraceId = appContext?.TraceId;
                metadata.SpanId = appContext?.SpanId;
                metadata.TenantId = appContext?.TenantId;
                metadata.Headers["Source"] = "Products.Api";
                metadata.Headers["Entity"] = nameof(Product);
                metadata.Headers["EventType"] = nameof(ProductCreatedIntegrationEvent);
            },
            cancellationToken);

        await logger.LogInformationAsync(
            "Product created",
            "product_created",
            new Dictionary<string, object?>
            {
                ["productId"] = normalizedProduct.Id,
                ["sku"] = normalizedProduct.Sku,
                ["eventId"] = productCreated.EventId,
                ["topic"] = ProductCreatedIntegrationEvent.Topic
            },
            cancellationToken);

        var stockQuantity = await inventoryStockAdapter.GetStockQuantityAsync(normalizedProduct.Id, cancellationToken) ?? request.StockQuantity;
        return Result<ProductResponse>.Success(normalizedProduct.ToResponse(stockQuantity));
    }
}
