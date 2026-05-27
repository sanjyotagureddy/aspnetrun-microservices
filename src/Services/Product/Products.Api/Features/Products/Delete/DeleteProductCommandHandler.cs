namespace Products.Api.Features.Products.Delete;

using Common.SharedKernel.Logging;

internal sealed class DeleteProductCommandHandler(IProductCatalogStore store, ILogger<DeleteProductCommandHandler> logger)
    : IRequestHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var deleted = await store.DeleteAsync(request.Id, cancellationToken);
        if (!deleted)
        {
            throw new Common.SharedKernel.Exceptions.NotFoundException(nameof(Product), request.Id.ToString());
        }

        await logger.LogInformationAsync(
            "Product deleted",
            new Dictionary<string, object?> { ["productId"] = request.Id },
            cancellationToken);

        return Result.Success();
    }
}
