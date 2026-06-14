using Auth.Api.Contracts;
using Auth.Api.Features.Auth.Health;
using Common.SharedKernel.Results;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Auth.Api.Tests.Features.Auth.Health;

public sealed class GetAuthHealthQueryHandlerTests
{
    [Fact]
    public async Task Handle_Should_Return_Healthy_Result_When_All_Readiness_Checks_Are_Healthy()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DateTime utcNow = new(2026, 6, 14, 12, 0, 0, DateTimeKind.Utc);
        TestTimeProvider timeProvider = new(utcNow);

        HealthReport report = new(
            new Dictionary<string, HealthReportEntry>
            {
                ["auth-db"] = new(HealthStatus.Healthy, "ok", TimeSpan.FromMilliseconds(1), exception: null, data: new Dictionary<string, object>(), tags: ["ready"])
            },
            TimeSpan.FromMilliseconds(1));

        FakeHealthCheckService healthService = new(report);
        GetAuthHealthQueryHandler handler = new(timeProvider, healthService);

        Result<AuthHealthResponse> result = await handler.Handle(new GetAuthHealthQuery(), cancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be("healthy");
        result.Value.UtcNow.Should().Be(utcNow);
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_When_Readiness_Check_Is_Not_Healthy()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        TestTimeProvider timeProvider = new(new DateTime(2026, 6, 14, 12, 30, 0, DateTimeKind.Utc));

        HealthReport report = new(
            new Dictionary<string, HealthReportEntry>
            {
                ["auth-db"] = new(HealthStatus.Unhealthy, "db-down", TimeSpan.FromMilliseconds(1), exception: null, data: new Dictionary<string, object>(), tags: ["ready"])
            },
            TimeSpan.FromMilliseconds(1));

        FakeHealthCheckService healthService = new(report);
        GetAuthHealthQueryHandler handler = new(timeProvider, healthService);

        Result<AuthHealthResponse> result = await handler.Handle(new GetAuthHealthQuery(), cancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("auth-db:Unhealthy");
    }

    private sealed class FakeHealthCheckService(HealthReport report) : HealthCheckService
    {
        public override Task<HealthReport> CheckHealthAsync(Func<HealthCheckRegistration, bool>? predicate, CancellationToken cancellationToken = default)
            => Task.FromResult(report);
    }

    private sealed class TestTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
