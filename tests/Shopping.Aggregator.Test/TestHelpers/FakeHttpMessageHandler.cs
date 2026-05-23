using System.Net;
using System.Text;

namespace Shopping.Aggregator.Test.TestHelpers;

internal sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
{
  private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory = responseFactory ?? throw new ArgumentNullException(nameof(responseFactory));

  public List<HttpRequestMessage> Requests { get; } = new();

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    Requests.Add(request);
    return Task.FromResult(_responseFactory(request));
  }

  public static HttpResponseMessage JsonResponse<T>(T value, HttpStatusCode statusCode = HttpStatusCode.OK)
  {
    var payload = System.Text.Json.JsonSerializer.Serialize(value);
    return new HttpResponseMessage(statusCode)
    {
      Content = new StringContent(payload, Encoding.UTF8, "application/json")
    };
  }
}