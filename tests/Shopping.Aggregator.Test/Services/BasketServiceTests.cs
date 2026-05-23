using System.Net;

using NUnit.Framework;

using Shopping.Aggregator.Models;
using Shopping.Aggregator.Services;
using Shopping.Aggregator.Test.TestHelpers;

namespace Shopping.Aggregator.Test.Services;

public class BasketServiceTests
{
  [Test]
  public async Task GetBasket_CallsBasketEndpoint_AndReturnsBasket()
  {
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse(new BasketModel { UserName = "swn", TotalPrice = 25 }));
    var client = new HttpClient(handler)
    {
      BaseAddress = new Uri("http://shopping.local")
    };
    var service = new BasketService(client);

    BasketModel basket = await service.GetBasket("swn");

    Assert.That(handler.Requests, Has.Count.EqualTo(1));
    Assert.That(handler.Requests[0].Method, Is.EqualTo(HttpMethod.Get));
    Assert.That(handler.Requests[0].RequestUri!.PathAndQuery, Is.EqualTo("/api/v1/Basket/swn"));
    Assert.That(basket.UserName, Is.EqualTo("swn"));
    Assert.That(basket.TotalPrice, Is.EqualTo(25));
  }
}