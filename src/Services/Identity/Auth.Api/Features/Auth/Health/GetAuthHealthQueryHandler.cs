using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Auth.Api.Features.Auth.Health;

internal sealed class GetAuthHealthQueryHandler(
    TimeProvider timeProvider,
    HealthCheckService healthCheckService) : IRequestHandler<GetAuthHealthQuery, Result<AuthHealthResponse>>
{
    public async Task<Result<AuthHealthResponse>> Handle(GetAuthHealthQuery request, CancellationToken cancellationToken)
    {
        HealthReport healthReport = await healthCheckService.CheckHealthAsync(
            registration => registration.Tags.Contains("ready"),
            cancellationToken);

        if (healthReport.Status != HealthStatus.Healthy)
        {
            string failedChecks = string.Join(", ",
                healthReport.Entries
                    .Where(entry => entry.Value.Status != HealthStatus.Healthy)
                    .Select(entry => $"{entry.Key}:{entry.Value.Status}"));

            string detail = string.IsNullOrWhiteSpace(failedChecks)
                ? "One or more readiness dependencies are unavailable."
                : $"Readiness checks failed: {failedChecks}.";

            return Result<AuthHealthResponse>.Failure(detail);
        }

        AuthHealthResponse response = new("auth-api", "healthy", timeProvider.GetUtcNow().UtcDateTime);
        return Result<AuthHealthResponse>.Success(response);
    }
}
