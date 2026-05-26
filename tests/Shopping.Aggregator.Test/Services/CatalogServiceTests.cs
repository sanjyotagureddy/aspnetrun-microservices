using Shopping.Aggregator.Models;
using Shopping.Aggregator.Services;
using Shopping.Aggregator.Test.TestHelpers;
using Xunit;

namespace Shopping.Aggregator.Test.Services;

public class CatalogServiceTests
{
  [Fact]
  public async Task GetCatalog_CallsCollectionEndpoint()
  {
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse(new List<CatalogModel>
    {
      new() { Id = "1", Name = "Phone", Category = "Electronics" }
    }));
    var client = new HttpClient(handler) { BaseAddress = new Uri("http://shopping.local") };
    var service = new CatalogService(client);

    IEnumerable<CatalogModel> catalog = await service.GetCatalog();

    Assert.Equal("/api/v1/catalog/products", handler.Requests[0].RequestUri!.PathAndQuery);
    Assert.Single(catalog);
  }

  [Fact]
  public async Task GetCatalogByCategory_EncodesCategoryInQueryString()
  {
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse(new List<CatalogModel>()));
    var client = new HttpClient(handler) { BaseAddress = new Uri("http://shopping.local") };
    var service = new CatalogService(client);

    await service.GetCatalogByCategory("home appliances");

    Assert.Equal("/api/v1/catalog/products?category=home%20appliances", handler.Requests[0].RequestUri!.PathAndQuery);
  }

  [Fact]
  public async Task GetProductByName_EncodesNameInQueryString()
  {
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse(new List<CatalogModel>()));
    var client = new HttpClient(handler) { BaseAddress = new Uri("http://shopping.local") };
    var service = new CatalogService(client);

    await service.GetProductByName("folding chair");

    Assert.Equal("/api/v1/catalog/products?name=folding%20chair", handler.Requests[0].RequestUri!.PathAndQuery);
  }

  [Fact]
  public async Task GetCatalogById_CallsItemEndpoint()
  {
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse(new CatalogModel { Id = "abc", Name = "Phone" }));
    var client = new HttpClient(handler) { BaseAddress = new Uri("http://shopping.local") };
    var service = new CatalogService(client);

    CatalogModel catalog = await service.GetCatalog("abc");

    Assert.Equal("/api/v1/catalog/products/abc", handler.Requests[0].RequestUri!.PathAndQuery);
    Assert.Equal("abc", catalog.Id);
  }
}