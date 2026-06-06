namespace Inventory.Api.Features.Inventory.Initialize;

internal sealed class InitializeInventoryCommandHandler(
    IInventoryStore store,
    TimeProvider timeProvider,
    Common.SharedKernel.Logging.ILogger<InitializeInventoryCommandHandler> logger)
    : IRequestHandler<InitializeInventoryCommand, Result>
{
    public async Task<Result> Handle(InitializeInventoryCommand request, CancellationToken cancellationToken)
    {
        Observability.AppCallContext? appContext = Common.SharedKernel.Observability.Context.AppCallContextBase.CurrentAs<Observability.AppCallContext>();
        DateTime occurredOnUtc = timeProvider.GetUtcNow().UtcDateTime;
        InventoryItem item = new(request.ProductId, request.StockQuantity, occurredOnUtc);

        await store.InitializeAsync(item, cancellationToken);

        await logger.LogInformationAsync(
            "Inventory initialized",
            new Dictionary<string, object?>
            {
                ["productId"] = request.ProductId,
                ["stockQuantity"] = request.StockQuantity
            },
            cancellationToken);

        return Result.Success();
    }
}
