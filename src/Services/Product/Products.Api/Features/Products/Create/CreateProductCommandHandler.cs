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
        int confirmedStockQuantity;

        await store.EnsureSkuIsUniqueAsync(normalizedProduct.Sku, null, cancellationToken);
        await store.AddAsync(normalizedProduct, cancellationToken);

        try
        {
            await inventoryStockAdapter.InitializeAsync(normalizedProduct.Id, request.StockQuantity, cancellationToken);
            confirmedStockQuantity = await inventoryStockAdapter.GetStockQuantityAsync(normalizedProduct.Id, cancellationToken) ?? request.StockQuantity;
        }
        catch (Exception ex)
        {
            bool rollbackSucceeded;

            try
            {
                rollbackSucceeded = await store.DeleteAsync(normalizedProduct.Id, cancellationToken);
            }
            catch (Exception rollbackException)
            {
                await logger.LogCriticalAsync(
                    "Product rollback failed after inventory initialization error",
                    exception: rollbackException,
                    cancellationToken: cancellationToken);

                throw new Common.SharedKernel.Exceptions.ConflictException("Product creation failed because inventory initialization could not be completed.");
            }

            await logger.LogWarningAsync(
                "Inventory initialization failed for product; create operation was rolled back",
                "inventory_initialize_failed_product_rolled_back",
                new Dictionary<string, object?>
                {
                    ["productId"] = normalizedProduct.Id,
                    ["stockQuantity"] = request.StockQuantity,
                    ["exceptionType"] = ex.GetType().Name,
                    ["rollbackSucceeded"] = rollbackSucceeded
                },
                cancellationToken);

            throw new Common.SharedKernel.Exceptions.ConflictException("Product creation failed because inventory initialization could not be completed.");
        }

        ProductCreatedIntegrationEvent productCreated = normalizedProduct.ToCreatedIntegrationEvent(occurredOnUtc, confirmedStockQuantity);
        AppCallContext? appContext = AppCallContextBase.CurrentAs<AppCallContext>();

        await messageBus.PublishAsync(
            ProductCreatedIntegrationEvent.Topic,
            productCreated,
            metadata =>
            {
                metadata.MessageId = productCreated.EventId.ToString("N");
                metadata.OrderingKey = normalizedProduct.Id.ToString("N");
                metadata.Contract = new MessageContractDescriptor(nameof(ProductCreatedIntegrationEvent), "1.0", "application/json");
                metadata.CorrelationId = appContext?.CorrelationId;
                metadata.TraceId = appContext?.TraceId;
                metadata.SpanId = appContext?.SpanId;
                metadata.TenantId = appContext?.TenantId;
                metadata.Headers["Source"] = "products-api";
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
                ["stockQuantity"] = confirmedStockQuantity,
                ["eventId"] = productCreated.EventId,
                ["topic"] = ProductCreatedIntegrationEvent.Topic
            },
            cancellationToken);

        return Result<ProductResponse>.Success(normalizedProduct.ToResponse(confirmedStockQuantity));
    }
}
