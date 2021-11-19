using Catalog.API.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Catalog.API.Repositories.Interfaces;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetProducts();

    Task<Product> GetProduct(string id);

    Task<IEnumerable<Product>> GetProductsByName(string name);

    Task<IEnumerable<Product>> GetProductsByCategory(string category);

    Task CreateProduct(Product product);

    Task<bool> UpdateProduct(Product product);

    Task<bool> DeleteProduct(string id);
}