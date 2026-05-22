using Catalog.API.Infrastructure.Persistence.Models;
using MongoDB.Driver;

namespace Catalog.API.Infrastructure.Persistence;

internal sealed class CatalogContext : ICatalogContext
{
    public CatalogContext(IConfiguration configuration)
    {
        var client = new MongoClient(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));
        IMongoDatabase database = client.GetDatabase(configuration.GetValue<string>("DatabaseSettings:DatabaseName"));

        Products = database.GetCollection<ProductDocument>(configuration.GetValue<string>("DatabaseSettings:CollectionName"));
        CatalogContextSeed.SeedData(Products);
    }

    public IMongoCollection<ProductDocument> Products { get; }
}