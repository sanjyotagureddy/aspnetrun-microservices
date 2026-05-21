using Catalog.API.Application.Contracts.Persistence;
using MediatR;

namespace Catalog.API.Application.Features.Products.Commands.UpdateProduct;

internal sealed class UpdateProductCommandHandler(IProductRepository repository) : IRequestHandler<UpdateProductCommand, bool>
{
    private readonly IProductRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public Task<bool> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        return _repository.UpdateProduct(request.Product, cancellationToken);
    }
}