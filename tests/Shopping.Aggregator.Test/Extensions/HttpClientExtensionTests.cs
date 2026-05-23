using System.Net;
using System.Net.Http;
using System.Text;

using NUnit.Framework;

using Shopping.Aggregator.Extensions;
using Shopping.Aggregator.Models;
using Shopping.Aggregator.Test.TestHelpers;

namespace Shopping.Aggregator.Test.Extensions;

public class HttpClientExtensionTests
{
  [Test]
  public async Task ReadContentAs_Returns_DeserializedPayload()
  {
    var response = new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent("{\"userName\":\"swn\",\"totalPrice\":1100}", Encoding.UTF8, "application/json")
    };

    BasketModel basket = await response.ReadContentAs<BasketModel>();

    Assert.That(basket.UserName, Is.EqualTo("swn"));
    Assert.That(basket.TotalPrice, Is.EqualTo(1100));
  }

  [Test]
  public void ReadContentAs_Throws_When_ResponseIsNotSuccess()
  {
    var response = new HttpResponseMessage(HttpStatusCode.BadGateway)
    {
      ReasonPhrase = "Bad Gateway"
    };

    var ex = Assert.ThrowsAsync<ApplicationException>(async () => await response.ReadContentAs<BasketModel>());

    Assert.That(ex!.Message, Does.Contain("Something went wrong calling the API"));
    Assert.That(ex.Message, Does.Contain("Bad Gateway"));
  }
}