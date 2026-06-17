using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Auth.Api.Infrastructure.Health;

internal sealed class AuthCacheHealthCheck(IConfiguration configuration) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        string? cacheConnection = configuration.GetConnectionString("auth-cache");

        return Task.FromResult(string.IsNullOrWhiteSpace(cacheConnection)
            ? HealthCheckResult.Degraded("Auth cache connection string is not configured.")
            : HealthCheckResult.Healthy("Auth cache connection string is configured."));
    }
}
