using System.Net;

using Shopping.Aggregator.Models;
using Shopping.Aggregator.Services;
using Shopping.Aggregator.Test.TestHelpers;
using Xunit;

namespace Shopping.Aggregator.Test.Services;

public class BasketServiceTests
{
  [Fact]
  public async Task GetBasket_CallsBasketEndpoint_AndReturnsBasket()
  {
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse(new BasketModel { UserName = "swn", TotalPrice = 25 }));
    var client = new HttpClient(handler)
    {
      BaseAddress = new Uri("http://shopping.local")
    };
    var service = new BasketService(client);

    BasketModel basket = await service.GetBasket("swn");

    Assert.Single(handler.Requests);
    Assert.Equal(HttpMethod.Get, handler.Requests[0].Method);
    Assert.Equal("/api/v1/Basket/swn", handler.Requests[0].RequestUri!.PathAndQuery);
    Assert.Equal("swn", basket.UserName);
    Assert.Equal(25, basket.TotalPrice);
  }
}