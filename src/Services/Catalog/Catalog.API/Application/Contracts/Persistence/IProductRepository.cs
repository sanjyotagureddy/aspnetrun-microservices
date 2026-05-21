using Catalog.API.Domain.Entities;

namespace Catalog.API.Application.Contracts.Persistence;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetProducts(CancellationToken cancellationToken = default);

    Task<Product?> GetProduct(Guid id, CancellationToken cancellationToken = default);

    Task<IEnumerable<Product>> GetProductsByName(string name, CancellationToken cancellationToken = default);

    Task<IEnumerable<Product>> GetProductsByCategory(string category, CancellationToken cancellationToken = default);

    Task CreateProduct(Product product, CancellationToken cancellationToken = default);

    Task<bool> UpdateProduct(Product product, CancellationToken cancellationToken = default);

    Task<bool> DeleteProduct(Guid id, CancellationToken cancellationToken = default);
}