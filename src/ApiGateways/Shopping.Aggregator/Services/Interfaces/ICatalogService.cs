using System.Collections.Generic;
using System.Threading.Tasks;
using Shopping.Aggregator.Models;

namespace Shopping.Aggregator.Services.Interfaces;

public interface ICatalogService
{
    Task<IEnumerable<CatalogModel>> GetCatalog();

    Task<IEnumerable<CatalogModel>> GetCatalogByCategory(string category);

    Task<IEnumerable<CatalogModel>> GetProductByName(string name);

    Task<CatalogModel> GetCatalog(string id);
}