using Shopping.Aggregator.Models;
using Shopping.Aggregator.Services;
using Shopping.Aggregator.Test.TestHelpers;
using Xunit;

namespace Shopping.Aggregator.Test.Services;

public class OrderServiceTests
{
  [Fact]
  public async Task GetOrdersByUserName_CallsOrderEndpoint()
  {
    var handler = new FakeHttpMessageHandler(_ => FakeHttpMessageHandler.JsonResponse(new List<OrderResponseModel>
    {
      new() { UserName = "swn", TotalPrice = 55 }
    }));
    var client = new HttpClient(handler) { BaseAddress = new Uri("http://shopping.local") };
    var service = new OrderService(client);

    IEnumerable<OrderResponseModel> orders = await service.GetOrdersByUserName("swn");

    Assert.Equal("/api/v1/Order/swn", handler.Requests[0].RequestUri!.PathAndQuery);
    Assert.Single(orders);
  }
}