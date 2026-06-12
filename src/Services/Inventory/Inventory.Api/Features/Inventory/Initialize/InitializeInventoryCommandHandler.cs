namespace Inventory.Api.Features.Inventory.Initialize;

using Common.SharedKernel.Logging;

internal sealed class InitializeInventoryCommandHandler(
    IInventoryStore store,
    TimeProvider timeProvider,
    Common.SharedKernel.Logging.ILogger<InitializeInventoryCommandHandler> logger)
    : IRequestHandler<InitializeInventoryCommand, Result>
{
    public async Task<Result> Handle(InitializeInventoryCommand request, CancellationToken cancellationToken)
    {
        DateTime occurredOnUtc = timeProvider.GetUtcNow().UtcDateTime;
        InventoryItem item = new(request.ProductId, request.StockQuantity, occurredOnUtc);

        await store.InitializeAsync(item, cancellationToken);

        await logger.LogTraceAsync(
            new TraceLog
            {
                Message = "Inventory initialized",
                Context = new Dictionary<string, object?>
                {
                    ["productId"] = request.ProductId,
                    ["stockQuantity"] = request.StockQuantity
                }
            },
            LogType.Application,
            cancellationToken);

        return Result.Success();
    }
}
