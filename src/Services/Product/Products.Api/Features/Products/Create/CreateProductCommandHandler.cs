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
    IProductDomainEventDispatcher domainEventDispatcher,
    IProductTransactionExecutor transactionExecutor)
    : IRequestHandler<CreateProductCommand, Result<ProductResponse>>
{
    public async Task<Result<ProductResponse>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        DateTime occurredOnUtc = timeProvider.GetUtcNow().UtcDateTime;
        Product normalizedProduct = request.ToDomain(Guid.NewGuid(), occurredOnUtc);

        try
        {
            await transactionExecutor.ExecuteAsync(async (connection, transaction, ct) =>
            {
                await store.EnsureSkuIsUniqueAsync(normalizedProduct.Sku, null, connection, transaction, ct);
                await store.AddAsync(normalizedProduct, connection, transaction, ct);

                AppCallContext? appContext = AppCallContextBase.CurrentAs<AppCallContext>();
                normalizedProduct.RaiseCreatedDomainEvent(request.StockQuantity, occurredOnUtc);
                await domainEventDispatcher.DispatchAsync(normalizedProduct.DomainEvents, appContext, connection, transaction, ct);
                normalizedProduct.ClearDomainEvents();
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            await logger.LogApplicationAsync(
                new ErrorLog
                {
                    Message = "Product creation failed while writing product and outbox state",
                    Category = "product_create_transaction_failed",
                    Exception = ex,
                    ExceptionType = ex.GetType().FullName,
                    ExceptionMessage = ex.Message,
                    Context = new Dictionary<string, object?>
                    {
                        ["operation"] = "product.create",
                        ["aggregateType"] = "product",
                        ["productId"] = normalizedProduct.Id,
                        ["stockQuantity"] = request.StockQuantity,
                        ["eventType"] = ProductCreatedDomainEvent.EventTypeName,
                        ["topic"] = ProductCreatedIntegrationEvent.Topic,
                        ["occurredOnUtc"] = occurredOnUtc
                    }
                },
                cancellationToken);

            throw new Common.SharedKernel.Exceptions.ConflictException("Product creation failed and was rolled back.");
        }

        int confirmedStockQuantity = request.StockQuantity;

        try
        {
            await inventoryStockAdapter.InitializeAsync(normalizedProduct.Id, request.StockQuantity, cancellationToken);
            confirmedStockQuantity = await inventoryStockAdapter.GetStockQuantityAsync(normalizedProduct.Id, cancellationToken) ?? request.StockQuantity;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            await logger.LogApplicationAsync(
                new ErrorLog
                {
                    Message = "Product committed but inventory initialization failed",
                    Category = "product_create_inventory_initialization_failed",
                    Exception = ex,
                    ExceptionType = ex.GetType().FullName,
                    ExceptionMessage = ex.Message,
                    Context = new Dictionary<string, object?>
                    {
                        ["operation"] = "product.create",
                        ["aggregateType"] = "product",
                        ["productId"] = normalizedProduct.Id,
                        ["stockQuantity"] = request.StockQuantity,
                        ["eventType"] = ProductCreatedDomainEvent.EventTypeName,
                        ["topic"] = ProductCreatedIntegrationEvent.Topic,
                        ["occurredOnUtc"] = occurredOnUtc
                    }
                },
                cancellationToken);
        }

        await logger.LogApplicationAsync(
            new TraceLog
            {
                Message = "Product created",
                Category = "product_created",
                Operation = "product.create",
                Context = new Dictionary<string, object?>
                {
                    ["aggregateType"] = "product",
                    ["productId"] = normalizedProduct.Id,
                    ["sku"] = normalizedProduct.Sku,
                    ["stockQuantity"] = confirmedStockQuantity,
                    ["eventType"] = ProductCreatedDomainEvent.EventTypeName,
                    ["topic"] = ProductCreatedIntegrationEvent.Topic,
                    ["occurredOnUtc"] = occurredOnUtc
                }
            },
            cancellationToken);

        return Result<ProductResponse>.Success(normalizedProduct.ToResponse(confirmedStockQuantity));
    }
}
