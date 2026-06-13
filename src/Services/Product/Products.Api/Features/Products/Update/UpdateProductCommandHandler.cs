using Common.SharedKernel.Logging;
using Common.SharedKernel.Observability.Context;
using Products.Api.Domain.Events;
using Products.Api.Features.Products.Events;

namespace Products.Api.Features.Products.Update;

internal sealed class UpdateProductCommandHandler(
    IProductCatalogStore store,
    IInventoryStockAdapter inventoryStockAdapter,
    TimeProvider timeProvider,
    Common.SharedKernel.Logging.ILogger<UpdateProductCommandHandler> logger,
    IProductDomainEventDispatcher domainEventDispatcher,
    IProductTransactionExecutor transactionExecutor)
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
        int stockQuantity = await inventoryStockAdapter.GetStockQuantityAsync(normalizedProduct.Id, cancellationToken) ?? 0;

        await transactionExecutor.ExecuteAsync(async (connection, transaction, ct) =>
        {
            await store.EnsureSkuIsUniqueAsync(normalizedProduct.Sku, normalizedProduct.Id, connection, transaction, ct);
            await store.UpdateAsync(normalizedProduct, connection, transaction, ct);

            AppCallContextBase? appContext = AppCallContextBase.Current;
            normalizedProduct.RaiseUpdatedDomainEvent(stockQuantity, occurredOnUtc);
            await domainEventDispatcher.DispatchAsync(normalizedProduct.DomainEvents, appContext, connection, transaction, ct);
            normalizedProduct.ClearDomainEvents();
        }, cancellationToken);

        await logger.LogApplicationAsync(
            new TraceLog
            {
                Message = "Product updated",
                Category = "product_updated",
                Operation = "product.update",
                Context = new Dictionary<string, object?>
                {
                    ["aggregateType"] = "product",
                    ["productId"] = normalizedProduct.Id,
                    ["sku"] = normalizedProduct.Sku,
                    ["stockQuantity"] = stockQuantity,
                    ["eventType"] = ProductUpdatedDomainEvent.EventTypeName,
                    ["topic"] = ProductUpdatedIntegrationEvent.Topic,
                    ["occurredOnUtc"] = occurredOnUtc
                }
            },
            cancellationToken);

        return Result<ProductResponse>.Success(normalizedProduct.ToResponse(stockQuantity));
    }
}
