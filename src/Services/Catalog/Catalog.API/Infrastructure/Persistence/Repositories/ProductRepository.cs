using Catalog.API.Application.Contracts.Persistence;
using Catalog.API.Domain.Entities;
using Catalog.API.Infrastructure.Persistence.Models;
using MongoDB.Driver;

namespace Catalog.API.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository(ICatalogContext context) : IProductRepository
{
    public async Task CreateProduct(Product product, CancellationToken cancellationToken = default)
    {
        await context.Products.InsertOneAsync(ProductDocument.FromDomain(product), cancellationToken: cancellationToken);
    }

    public async Task<bool> DeleteProduct(Guid id, CancellationToken cancellationToken = default)
    {
        FilterDefinition<ProductDocument>? filter = Builders<ProductDocument>.Filter.Eq(p => p.Id, id);
        DeleteResult? deleteResult = await context.Products.DeleteOneAsync(filter, cancellationToken);

        return deleteResult.IsAcknowledged && deleteResult.DeletedCount > 0;
    }

    public async Task<Product?> GetProduct(Guid id, CancellationToken cancellationToken = default)
    {
        ProductDocument? product = await context.Products.Find(p => p.Id == id).FirstOrDefaultAsync(cancellationToken);
        return product is null ? null : product.ToDomain();
    }

    public async Task<IEnumerable<Product>> GetProducts(CancellationToken cancellationToken = default)
    {
        List<ProductDocument>? products = await context.Products.Find(_ => true).ToListAsync(cancellationToken);
        return products.Select(product => product.ToDomain());
    }

    public async Task<IEnumerable<Product>> GetProductsByCategory(string category, CancellationToken cancellationToken = default)
    {
        FilterDefinition<ProductDocument>? filter = Builders<ProductDocument>.Filter.Eq(p => p.Category, category);
        List<ProductDocument>? products = await context.Products.Find(filter).ToListAsync(cancellationToken);
        return products.Select(product => product.ToDomain());
    }

    public async Task<IEnumerable<Product>> GetProductsByName(string name, CancellationToken cancellationToken = default)
    {
        FilterDefinition<ProductDocument>? filter = Builders<ProductDocument>.Filter.Eq(p => p.Name, name);
        List<ProductDocument>? products = await context.Products.Find(filter).ToListAsync(cancellationToken);
        return products.Select(product => product.ToDomain());
    }

    public async Task<bool> UpdateProduct(Product product, CancellationToken cancellationToken = default)
    {
        ReplaceOneResult? updateresult = await context.Products.ReplaceOneAsync(
            g => g.Id == product.Id,
            ProductDocument.FromDomain(product),
            cancellationToken: cancellationToken);

        return updateresult.IsAcknowledged && updateresult.ModifiedCount > 0;
    }
}