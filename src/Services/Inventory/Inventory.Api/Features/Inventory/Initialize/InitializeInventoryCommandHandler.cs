namespace Inventory.Api.Features.Inventory.Initialize;

using Common.SharedKernel.Logging;
using Common.SharedKernel.Observability.Context;
using InventoryInitializedDomainEvent = global::Inventory.Api.Domain.Events.InventoryInitializedDomainEvent;
using InventoryInitializedIntegrationEvent = global::Inventory.Api.Features.Inventory.Events.InventoryInitializedIntegrationEvent;

internal sealed class InitializeInventoryCommandHandler(
    IInventoryStore store,
    TimeProvider timeProvider,
    ILogger<InitializeInventoryCommandHandler> logger,
    IInventoryDomainEventDispatcher domainEventDispatcher,
    IInventoryTransactionExecutor transactionExecutor)
    : IRequestHandler<InitializeInventoryCommand, Result>
{
    public async Task<Result> Handle(InitializeInventoryCommand request, CancellationToken cancellationToken)
    {
        DateTime occurredOnUtc = timeProvider.GetUtcNow().UtcDateTime;
        InventoryItem item = new(request.ProductId, request.StockQuantity, occurredOnUtc);

        try
        {
            await transactionExecutor.ExecuteAsync(async (connection, transaction, ct) =>
            {
                await store.InitializeAsync(item, connection, transaction, ct);

                AppCallContextBase? appContext = AppCallContextBase.Current;
                item.RaiseInitializedDomainEvent(occurredOnUtc);
                await domainEventDispatcher.DispatchAsync(item.DomainEvents, appContext, connection, transaction, ct);
                item.ClearDomainEvents();
            }, cancellationToken);
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
                    Message = "Inventory initialization failed while writing inventory and outbox state",
                    Category = "inventory_initialize_transaction_failed",
                    Exception = ex,
                    ExceptionType = ex.GetType().FullName,
                    ExceptionMessage = ex.Message,
                    Context = new Dictionary<string, object?>
                    {
                        ["operation"] = "inventory.initialize",
                        ["productId"] = request.ProductId,
                        ["stockQuantity"] = request.StockQuantity,
                        ["eventType"] = InventoryInitializedDomainEvent.EventTypeName,
                        ["topic"] = InventoryInitializedIntegrationEvent.Topic,
                        ["occurredOnUtc"] = occurredOnUtc
                    }
                },
                cancellationToken);

            throw;
        }

        await logger.LogApplicationAsync(
            new TraceLog
            {
                Message = "Inventory initialized",
                Category = "inventory_initialized",
                Operation = "inventory.initialize",
                Context = new Dictionary<string, object?>
                {
                    ["aggregateType"] = "inventory-item",
                    ["productId"] = request.ProductId,
                    ["stockQuantity"] = request.StockQuantity,
                    ["eventType"] = InventoryInitializedDomainEvent.EventTypeName,
                    ["topic"] = InventoryInitializedIntegrationEvent.Topic,
                    ["occurredOnUtc"] = occurredOnUtc
                }
            },
            cancellationToken);

        return Result.Success();
    }
}
