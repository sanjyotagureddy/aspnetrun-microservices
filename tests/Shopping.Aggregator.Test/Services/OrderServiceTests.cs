using NUnit.Framework;

using Shopping.Aggregator.Models;
using Shopping.Aggregator.Services;
using Shopping.Aggregator.Test.TestHelpers;

namespace Shopping.Aggregator.Test.Services;

public class OrderServiceTests
{
  [Test]
  public async Task GetOrdersByUserName_CallsOrderEndpoint()
  {
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse(new List<OrderResponseModel>
    {
      new() { UserName = "swn", TotalPrice = 55 }
    }));
    var client = new HttpClient(handler) { BaseAddress = new Uri("http://shopping.local") };
    var service = new OrderService(client);

    IEnumerable<OrderResponseModel> orders = await service.GetOrdersByUserName("swn");

    Assert.That(handler.Requests[0].RequestUri!.PathAndQuery, Is.EqualTo("/api/v1/Order/swn"));
    Assert.That(orders, Has.Count.EqualTo(1));
  }
}