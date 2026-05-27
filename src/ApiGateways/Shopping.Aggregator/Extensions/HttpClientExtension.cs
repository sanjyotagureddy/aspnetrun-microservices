using System.Text.Json;
using SharedKernel.Errors;

namespace Shopping.Aggregator.Extensions;

public static class HttpClientExtension
{
  public static async Task<T> ReadContentAs<T>(this HttpResponseMessage response)
  {
    if (!response.IsSuccessStatusCode)
      throw Errors.ServerSide.DependencyFailure($"Something went wrong calling the API: {response.ReasonPhrase}");

    var dataAsString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

    return JsonSerializer.Deserialize<T>(dataAsString,
      new JsonSerializerOptions { PropertyNameCaseInsensitive = true, });
  }
}
