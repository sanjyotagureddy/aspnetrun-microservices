using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Auth.Api.Infrastructure.Health;

internal sealed class AuthAuthorityHealthCheck(IHttpClientFactory httpClientFactory, IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        string? discoveryUrl = configuration["Auth:DiscoveryUrl"];
        if (string.IsNullOrWhiteSpace(discoveryUrl))
        {
            return HealthCheckResult.Unhealthy("Auth discovery URL is not configured.");
        }

        HttpClient client = httpClientFactory.CreateClient("auth-authority-health");

        try
        {
            using HttpRequestMessage request = new(HttpMethod.Get, discoveryUrl);
            using HttpResponseMessage response = await client.SendAsync(request, cancellationToken);

            return response.IsSuccessStatusCode
                ? HealthCheckResult.Healthy("Authority discovery endpoint is reachable.")
                : HealthCheckResult.Unhealthy($"Authority discovery endpoint returned {(int)response.StatusCode}.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Authority discovery endpoint check failed.", ex);
        }
    }
}
