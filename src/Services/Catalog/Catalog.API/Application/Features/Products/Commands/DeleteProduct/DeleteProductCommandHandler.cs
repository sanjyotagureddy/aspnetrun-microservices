using Catalog.API.Application.Contracts.Persistence;
using MediatR;

namespace Catalog.API.Application.Features.Products.Commands.DeleteProduct;

internal sealed class DeleteProductCommandHandler(IProductRepository repository) : IRequestHandler<DeleteProductCommand, bool>
{
    private readonly IProductRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        return _repository.DeleteProduct(request.Id, cancellationToken);
    }
}