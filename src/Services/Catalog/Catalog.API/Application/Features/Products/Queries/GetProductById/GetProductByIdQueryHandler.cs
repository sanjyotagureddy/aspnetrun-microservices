using Catalog.API.Application.Contracts.Persistence;
using Catalog.API.Domain.Entities;
using MediatR;

namespace Catalog.API.Application.Features.Products.Queries.GetProductById;

internal sealed class GetProductByIdQueryHandler(IProductRepository repository) : IRequestHandler<GetProductByIdQuery, Product?>
{
    private readonly IProductRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public Task<Product?> Handle(GetProductByIdQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetProduct(request.Id, cancellationToken);
    }
}