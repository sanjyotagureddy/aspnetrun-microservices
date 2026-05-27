using System.Net;
using System.Text;
using SharedKernel.Exceptions;
using Shopping.Aggregator.Extensions;
using Shopping.Aggregator.Models;
using Xunit;

namespace Shopping.Aggregator.Test.Extensions;

public class HttpClientExtensionTests
{
  [Fact]
  public async Task ReadContentAs_Returns_DeserializedPayload()
  {
    var response = new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent("{\"userName\":\"swn\",\"totalPrice\":1100}", Encoding.UTF8, "application/json")
    };

    BasketModel basket = await response.ReadContentAs<BasketModel>();

    Assert.Equal("swn", basket.UserName);
    Assert.Equal(1100, basket.TotalPrice);
  }

  [Fact]
  public async Task ReadContentAs_Throws_When_ResponseIsNotSuccess()
  {
    var response = new HttpResponseMessage(HttpStatusCode.BadGateway)
    {
      ReasonPhrase = "Bad Gateway"
    };

    var ex = await Assert.ThrowsAsync<DependencyFailureException>(async () => await response.ReadContentAs<BasketModel>());

    Assert.Contains("Something went wrong calling the API", ex.Message);
    Assert.Contains("Bad Gateway", ex.Message);
  }
}
