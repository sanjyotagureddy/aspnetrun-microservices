namespace Products.Api.Features.Products.Delete;

internal sealed class DeleteProductCommandHandler(IProductCatalogStore store)
    : IRequestHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var deleted = await store.DeleteAsync(request.Id, cancellationToken);
        return !deleted ? throw new Common.SharedKernel.Exceptions.NotFoundException(nameof(Product), request.Id.ToString()) : Result.Success();
    }
}
