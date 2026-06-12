using Common.SharedKernel.Logging;
using Common.SharedKernel.Observability.Context;
using Products.Api.Domain.Events;
using Products.Api.Features.Products.Events;
using Products.Api.Observability;

namespace Products.Api.Features.Products.Create;

internal sealed class CreateProductCommandHandler(
    IProductCatalogStore store,
    IInventoryStockAdapter inventoryStockAdapter,
    TimeProvider timeProvider,
    Common.SharedKernel.Logging.ILogger<CreateProductCommandHandler> logger,
    IProductDomainEventDispatcher domainEventDispatcher)
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
                await logger.LogApplicationAsync(
                    new ErrorLog
                    {
                        Message = "Product rollback failed after inventory initialization error",
                        Exception = rollbackException,
                        ExceptionType = rollbackException.GetType().FullName,
                        ExceptionMessage = rollbackException.Message
                    },
                    cancellationToken);

                throw new Common.SharedKernel.Exceptions.ConflictException("Product creation failed because inventory initialization could not be completed.");
            }

            await logger.LogApplicationAsync(
                new ErrorLog
                {
                    Message = "Inventory initialization failed for product; create operation was rolled back",
                    Category = "inventory_initialize_failed_product_rolled_back",
                    Exception = ex,
                    ExceptionType = ex.GetType().FullName,
                    ExceptionMessage = ex.Message,
                    Context = new Dictionary<string, object?>
                    {
                        ["productId"] = normalizedProduct.Id,
                        ["stockQuantity"] = request.StockQuantity,
                        ["rollbackSucceeded"] = rollbackSucceeded
                    }
                },
                cancellationToken);

            throw new Common.SharedKernel.Exceptions.ConflictException("Product creation failed because inventory initialization could not be completed.");
        }

        AppCallContext? appContext = AppCallContextBase.CurrentAs<AppCallContext>();
        normalizedProduct.RaiseCreatedDomainEvent(confirmedStockQuantity, occurredOnUtc);
        await domainEventDispatcher.DispatchAsync(normalizedProduct.DomainEvents, appContext, cancellationToken);
        normalizedProduct.ClearDomainEvents();

        await logger.LogApplicationAsync(
            new TraceLog
            {
                Message = "Product created",
                Category = "product_created",
                Context = new Dictionary<string, object?>
                {
                    ["productId"] = normalizedProduct.Id,
                    ["sku"] = normalizedProduct.Sku,
                    ["stockQuantity"] = confirmedStockQuantity,
                    ["eventType"] = ProductCreatedDomainEvent.EventTypeName,
                    ["topic"] = ProductCreatedIntegrationEvent.Topic
                }
            },
            cancellationToken);

        return Result<ProductResponse>.Success(normalizedProduct.ToResponse(confirmedStockQuantity));
    }
}
