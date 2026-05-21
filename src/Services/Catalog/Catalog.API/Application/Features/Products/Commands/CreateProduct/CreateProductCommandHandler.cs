using Catalog.API.Application.Contracts.Persistence;
using Catalog.API.Domain.Entities;
using MediatR;

namespace Catalog.API.Application.Features.Products.Commands.CreateProduct;

internal sealed class CreateProductCommandHandler(IProductRepository repository) : IRequestHandler<CreateProductCommand, Product>
{
    private readonly IProductRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

    public async Task<Product> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        Product product = new()
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Category = request.Category,
            Summary = request.Summary,
            Description = request.Description,
            ImageFile = request.ImageFile,
            Price = request.Price
        };

        await _repository.CreateProduct(product, cancellationToken);
        return product;
    }
}