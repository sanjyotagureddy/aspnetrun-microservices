using Catalog.API.Application.Contracts.Persistence;
using Catalog.API.Domain.Entities;
using MediatR;

namespace Catalog.API.Application.Features.Products.Queries.GetProducts;

internal sealed class GetProductsQueryHandler(IProductRepository repository) : IRequestHandler<GetProductsQuery, IEnumerable<Product>>
{
    private readonly IProductRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<IEnumerable<Product>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            return await _repository.GetProductsByName(request.Name, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            return await _repository.GetProductsByCategory(request.Category, cancellationToken);
        }

        return await _repository.GetProducts(cancellationToken);
    }
}
