using NUnit.Framework;

using Shopping.Aggregator.Models;
using Shopping.Aggregator.Services;
using Shopping.Aggregator.Test.TestHelpers;

namespace Shopping.Aggregator.Test.Services;

public class CatalogServiceTests
{
  [Test]
  public async Task GetCatalog_CallsCollectionEndpoint()
  {
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse(new List<CatalogModel>
    {
      new() { Id = "1", Name = "Phone", Category = "Electronics" }
    }));
    var client = new HttpClient(handler) { BaseAddress = new Uri("http://shopping.local") };
    var service = new CatalogService(client);

    IEnumerable<CatalogModel> catalog = await service.GetCatalog();

    Assert.That(handler.Requests[0].RequestUri!.PathAndQuery, Is.EqualTo("/api/v1/catalog/products"));
    Assert.That(catalog, Has.Count.EqualTo(1));
  }

  [Test]
  public async Task GetCatalogByCategory_EncodesCategoryInQueryString()
  {
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse(new List<CatalogModel>()));
    var client = new HttpClient(handler) { BaseAddress = new Uri("http://shopping.local") };
    var service = new CatalogService(client);

    await service.GetCatalogByCategory("home appliances");

    Assert.That(handler.Requests[0].RequestUri!.PathAndQuery, Is.EqualTo("/api/v1/catalog/products?category=home%20appliances"));
  }

  [Test]
  public async Task GetProductByName_EncodesNameInQueryString()
  {
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse(new List<CatalogModel>()));
    var client = new HttpClient(handler) { BaseAddress = new Uri("http://shopping.local") };
    var service = new CatalogService(client);

    await service.GetProductByName("folding chair");

    Assert.That(handler.Requests[0].RequestUri!.PathAndQuery, Is.EqualTo("/api/v1/catalog/products?name=folding%20chair"));
  }

  [Test]
  public async Task GetCatalogById_CallsItemEndpoint()
  {
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse(new CatalogModel { Id = "abc", Name = "Phone" }));
    var client = new HttpClient(handler) { BaseAddress = new Uri("http://shopping.local") };
    var service = new CatalogService(client);

    CatalogModel catalog = await service.GetCatalog("abc");

    Assert.That(handler.Requests[0].RequestUri!.PathAndQuery, Is.EqualTo("/api/v1/catalog/products/abc"));
    Assert.That(catalog.Id, Is.EqualTo("abc"));
  }
}