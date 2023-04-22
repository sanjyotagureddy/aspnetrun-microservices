using System.Collections.Generic;
using System.Threading.Tasks;
using Catalog.API.Data;
using Catalog.API.Entities;
using Catalog.API.Repositories.Interfaces;
using MongoDB.Driver;

namespace Catalog.API.Repositories;

public class ProductRepository : IProductRepository
{
  private readonly ICatalogContext _context;

  public ProductRepository(ICatalogContext context)
  {
    _context = context;
  }

  public async Task CreateProduct(Product product)
  {
    await _context.Products.InsertOneAsync(product);
  }

  public async Task<bool> DeleteProduct(string id)
  {
    var filter = Builders<Product>.Filter.Eq(p => p.Id, id);
    var deleteResult = await _context.Products.DeleteOneAsync(filter);

    return deleteResult.IsAcknowledged && deleteResult.DeletedCount > 0;
  }

  public async Task<Product> GetProduct(string id)
  {
    return await _context.Products.Find(p => p.Id == id).FirstOrDefaultAsync();
  }

  public async Task<IEnumerable<Product>> GetProducts()
  {
    return await _context.Products.Find(p => true).ToListAsync();
  }

  public async Task<IEnumerable<Product>> GetProductsByCategory(string category)
  {
    var filter = Builders<Product>.Filter.Eq(p => p.Category, category);
    return await _context.Products.Find(filter).ToListAsync();
  }

  public async Task<IEnumerable<Product>> GetProductsByName(string name)
  {
    var filter = Builders<Product>.Filter.Eq(p => p.Name, name);
    return await _context.Products.Find(filter).ToListAsync();
  }

  public async Task<bool> UpdateProduct(Product product)
  {
    var updateresult = await _context.Products.ReplaceOneAsync(g => g.Id == product.Id, product);

    return updateresult.IsAcknowledged && updateresult.ModifiedCount > 0;
  }
}