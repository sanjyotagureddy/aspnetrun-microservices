using System.Reflection;
using System.Security.Claims;
using Auth.Api.Contracts;
using Auth.Api.Features.Auth.Internal;
using Auth.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Auth.Api.Tests.Features.Auth.Internal;

public sealed class EvaluateTenantPolicyEndpointTests
{
    [Fact]
    public async Task HandleAsync_Should_Use_TenantMember_Service_Result()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DefaultHttpContext httpContext = CreateContext();
        FakeTenantAuthorizationService authorizationService = new()
        {
            TenantMemberDecision = PolicyDecision.Allow()
        };

        PolicyEvaluationRequest request = new(AuthPolicyNames.TenantMemberPolicy, "tenant-1", null);

        IResult result = await InvokeHandleAsync(request, httpContext, authorizationService, cancellationToken);

        Ok<PolicyEvaluationResponse> ok = result.Should().BeOfType<Ok<PolicyEvaluationResponse>>().Subject;
        ok.Value.Should().NotBeNull();
        PolicyEvaluationResponse payload = ok.Value!;
        payload.Allowed.Should().BeTrue();
        payload.Reason.Should().Be("allowed");
    }

    [Fact]
    public async Task HandleAsync_Should_Use_PlatformAdmin_Service_Result_With_Audit()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DefaultHttpContext httpContext = CreateContext();
        FakeTenantAuthorizationService authorizationService = new()
        {
            PlatformAdminDecision = PolicyDecision.Allow()
        };

        PolicyEvaluationAuditRequest audit = new("test-action", "integration-test", "incident-123", DateTimeOffset.UtcNow);
        PolicyEvaluationRequest request = new(AuthPolicyNames.PlatformAdminPolicy, "tenant-2", audit);

        IResult result = await InvokeHandleAsync(request, httpContext, authorizationService, cancellationToken);

        Ok<PolicyEvaluationResponse> ok = result.Should().BeOfType<Ok<PolicyEvaluationResponse>>().Subject;
        ok.Value.Should().NotBeNull();
        PolicyEvaluationResponse payload = ok.Value!;
        payload.Allowed.Should().BeTrue();
        payload.Reason.Should().Be("allowed");
    }

    [Fact]
    public async Task HandleAsync_Should_Deny_When_Policy_Is_Unsupported()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        DefaultHttpContext httpContext = CreateContext();
        FakeTenantAuthorizationService authorizationService = new();

        PolicyEvaluationRequest request = new("unsupported-policy", "tenant-3", null);

        IResult result = await InvokeHandleAsync(request, httpContext, authorizationService, cancellationToken);

        Ok<PolicyEvaluationResponse> ok = result.Should().BeOfType<Ok<PolicyEvaluationResponse>>().Subject;
        ok.Value.Should().NotBeNull();
        PolicyEvaluationResponse payload = ok.Value!;
        payload.Allowed.Should().BeFalse();
        payload.Reason.Should().Contain("Unsupported policy");
    }

    private static DefaultHttpContext CreateContext()
    {
        ClaimsIdentity identity = new(
        [
            new Claim("sub", "user-1")
        ],
            authenticationType: "test");

        DefaultHttpContext context = new()
        {
            User = new ClaimsPrincipal(identity)
        };

        return context;
    }

    private static async Task<IResult> InvokeHandleAsync(
        PolicyEvaluationRequest request,
        HttpContext httpContext,
        ITenantAuthorizationService authorizationService,
        CancellationToken cancellationToken)
    {
        MethodInfo method = typeof(EvaluateTenantPolicyEndpoint)
            .GetMethod("HandleAsync", BindingFlags.NonPublic | BindingFlags.Static)!;

        Task<IResult> task = (Task<IResult>)method.Invoke(null, [request, httpContext, authorizationService, cancellationToken])!;
        return await task;
    }

    private sealed class FakeTenantAuthorizationService : ITenantAuthorizationService
    {
        public PolicyDecision TenantMemberDecision { get; set; } = PolicyDecision.Deny("tenant-member-denied");
        public PolicyDecision CatalogWriteDecision { get; set; } = PolicyDecision.Deny("catalog-write-denied");
        public PolicyDecision CheckoutDecision { get; set; } = PolicyDecision.Deny("checkout-denied");
        public PolicyDecision PlatformAdminDecision { get; set; } = PolicyDecision.Deny("platform-admin-denied");

        public Task<PolicyDecision> EvaluateTenantMemberAsync(ClaimsPrincipal user, string targetTenantId, CancellationToken cancellationToken)
            => Task.FromResult(TenantMemberDecision);

        public Task<PolicyDecision> EvaluateCatalogWriteAsync(ClaimsPrincipal user, string targetTenantId, CancellationToken cancellationToken)
            => Task.FromResult(CatalogWriteDecision);

        public Task<PolicyDecision> EvaluateCheckoutAsync(ClaimsPrincipal user, string targetTenantId, CancellationToken cancellationToken)
            => Task.FromResult(CheckoutDecision);

        public Task<PolicyDecision> EvaluatePlatformAdminAsync(ClaimsPrincipal user, string targetTenantId, PolicyEvaluationAuditRequest? audit, CancellationToken cancellationToken)
            => Task.FromResult(PlatformAdminDecision);
    }
}
