using Shopping.Aggregator.Extensions;
using Shopping.Aggregator.Models;
using Shopping.Aggregator.Services.Interfaces;

namespace Shopping.Aggregator.Services;

public class OrderService(HttpClient httpClient) : IOrderService
{
  private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

  public async Task<IEnumerable<OrderResponseModel>> GetOrdersByUserName(string userName)
  {
    var response = await _httpClient.GetAsync($"/api/v1/Order/{userName}");
    return await response.ReadContentAs<List<OrderResponseModel>>();
  }
}