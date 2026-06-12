using Common.SharedKernel.Logging;
using Common.SharedKernel.Observability.Context;
using Products.Api.Domain.Events;
using Products.Api.Features.Products.Events;

namespace Products.Api.Features.Products.Delete;

internal sealed class DeleteProductCommandHandler(
    IProductCatalogStore store,
    TimeProvider timeProvider,
    Common.SharedKernel.Logging.ILogger<DeleteProductCommandHandler> logger,
    IProductDomainEventDispatcher domainEventDispatcher,
    IProductTransactionExecutor transactionExecutor)
    : IRequestHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        DateTime occurredOnUtc = timeProvider.GetUtcNow().UtcDateTime;

        await transactionExecutor.ExecuteAsync(async (connection, transaction, ct) =>
        {
            bool deleted = await store.DeleteAsync(request.Id, connection, transaction, ct);
            if (!deleted)
            {
                throw new Common.SharedKernel.Exceptions.NotFoundException(nameof(Product), request.Id.ToString());
            }

            AppCallContextBase? appContext = AppCallContextBase.Current;
            ProductDeletedDomainEvent deletedDomainEvent = new(occurredOnUtc, request.Id, occurredOnUtc);
            await domainEventDispatcher.DispatchAsync([deletedDomainEvent], appContext, connection, transaction, ct);
        }, cancellationToken);

        await logger.LogApplicationAsync(
            new TraceLog
            {
                Message = "Product deleted",
                Context = new Dictionary<string, object?>
                {
                    ["productId"] = request.Id,
                    ["eventType"] = ProductDeletedDomainEvent.EventTypeName,
                    ["topic"] = ProductDeletedIntegrationEvent.Topic
                }
            },
            cancellationToken);

        return Result.Success();
    }
}
