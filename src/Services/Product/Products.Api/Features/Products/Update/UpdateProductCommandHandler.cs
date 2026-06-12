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
    IProductDomainEventDispatcher domainEventDispatcher)
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
        AppCallContextBase? appContext = AppCallContextBase.Current;
        normalizedProduct.RaiseUpdatedDomainEvent(stockQuantity, occurredOnUtc);
        await domainEventDispatcher.DispatchAsync(normalizedProduct.DomainEvents, appContext, cancellationToken);
        normalizedProduct.ClearDomainEvents();

        await logger.LogApplicationAsync(
            new TraceLog
            {
                Message = "Product updated",
                Context = new Dictionary<string, object?>
                {
                    ["productId"] = normalizedProduct.Id,
                    ["sku"] = normalizedProduct.Sku,
                    ["eventType"] = ProductUpdatedDomainEvent.EventTypeName,
                    ["topic"] = ProductUpdatedIntegrationEvent.Topic
                }
            },
            cancellationToken);

        return Result<ProductResponse>.Success(normalizedProduct.ToResponse(stockQuantity));
    }
}
