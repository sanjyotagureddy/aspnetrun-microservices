using Catalog.API.Infrastructure.Persistence.Models;
using MongoDB.Driver;

namespace Catalog.API.Infrastructure.Persistence;

internal interface ICatalogContext
{
    IMongoCollection<ProductDocument> Products { get; }
}