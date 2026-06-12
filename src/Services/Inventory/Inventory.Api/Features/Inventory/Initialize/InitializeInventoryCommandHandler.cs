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

        await transactionExecutor.ExecuteAsync(async (connection, transaction, ct) =>
        {
            await store.InitializeAsync(item, connection, transaction, ct);

            AppCallContextBase? appContext = AppCallContextBase.Current;
            item.RaiseInitializedDomainEvent(occurredOnUtc);
            await domainEventDispatcher.DispatchAsync(item.DomainEvents, appContext, connection, transaction, ct);
            item.ClearDomainEvents();
        }, cancellationToken);

        await logger.LogApplicationAsync(
            new TraceLog
            {
                Message = "Inventory initialized",
                Context = new Dictionary<string, object?>
                {
                    ["productId"] = request.ProductId,
                    ["stockQuantity"] = request.StockQuantity,
                    ["eventType"] = InventoryInitializedDomainEvent.EventTypeName,
                    ["topic"] = InventoryInitializedIntegrationEvent.Topic
                }
            },
            cancellationToken);

        return Result.Success();
    }
}
