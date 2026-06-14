using System.Reflection;
using System.Security.Claims;
using System.Globalization;
using Auth.Api.Contracts;
using Auth.Api.Features.Auth.Internal;
using Auth.Api.Infrastructure.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace Auth.Api.Tests.Features.Auth.Internal;

public sealed class ValidateWorkloadEndpointTests
{
    [Fact]
    public void HandleAsync_Should_Return_BadRequest_When_ClientId_Claim_Is_Missing()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DefaultHttpContext context = new()
        {
            User = CreateUser([])
        };

        WorkloadAuthOptions options = new()
        {
            Clients =
            [
                new WorkloadClientOptions { ClientId = "workload-a", AllowedScopes = ["inventory.read"] }
            ]
        };

        object raw = InvokeHandleAsync(context, Options.Create(options), cancellationToken);

        ProblemHttpResult problem = GetProblem(raw);
        problem.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void HandleAsync_Should_Return_Forbidden_When_Client_Is_Not_Configured()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DefaultHttpContext context = new()
        {
            User = CreateUser([new Claim("azp", "unknown-client"), new Claim("scope", "inventory.read")])
        };

        WorkloadAuthOptions options = new()
        {
            Clients =
            [
                new WorkloadClientOptions { ClientId = "workload-a", AllowedScopes = ["inventory.read"] }
            ]
        };

        object raw = InvokeHandleAsync(context, Options.Create(options), cancellationToken);

        ProblemHttpResult problem = GetProblem(raw);
        problem.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public void HandleAsync_Should_Return_Ok_When_Client_And_Scope_Are_Valid()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        long iat = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds();
        long exp = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds();

        DefaultHttpContext context = new()
        {
            User = CreateUser(
            [
                new Claim("azp", "workload-a"),
                new Claim("scope", "inventory.read inventory.write"),
                new Claim("aud", "inventory-api"),
                new Claim("iat", iat.ToString(CultureInfo.InvariantCulture)),
                new Claim("exp", exp.ToString(CultureInfo.InvariantCulture))
            ])
        };

        WorkloadAuthOptions options = new()
        {
            Clients =
            [
                new WorkloadClientOptions { ClientId = "workload-a", AllowedScopes = ["inventory.read"] }
            ]
        };

        object raw = InvokeHandleAsync(context, Options.Create(options), cancellationToken);

        Ok<WorkloadValidationResponse> ok = GetOk(raw);
        ok.Value.Should().NotBeNull();
        WorkloadValidationResponse payload = ok.Value!;
        payload.ClientId.Should().Be("workload-a");
        payload.Scopes.Should().Contain("inventory.read");
        payload.Audiences.Should().Contain("inventory-api");
        payload.IssuedAtUtc.Should().NotBeNull();
        payload.ExpiresAtUtc.Should().NotBeNull();
    }

    private static object InvokeHandleAsync(HttpContext context, IOptions<WorkloadAuthOptions> options, CancellationToken cancellationToken)
    {
        MethodInfo method = typeof(ValidateWorkloadEndpoint)
            .GetMethod("HandleAsync", BindingFlags.NonPublic | BindingFlags.Static)!;

        return method.Invoke(null, [context, options, cancellationToken])!;
    }

    private static ProblemHttpResult GetProblem(object raw)
    {
        dynamic dynamicResult = raw;
        ProblemHttpResult? problem = dynamicResult.Result as ProblemHttpResult;
        problem.Should().NotBeNull();
        return problem!;
    }

    private static Ok<WorkloadValidationResponse> GetOk(object raw)
    {
        dynamic dynamicResult = raw;
        Ok<WorkloadValidationResponse>? ok = dynamicResult.Result as Ok<WorkloadValidationResponse>;
        ok.Should().NotBeNull();
        return ok!;
    }

    private static ClaimsPrincipal CreateUser(Claim[] claims)
    {
        ClaimsIdentity identity = new(claims, authenticationType: "test");
        return new ClaimsPrincipal(identity);
    }
}
