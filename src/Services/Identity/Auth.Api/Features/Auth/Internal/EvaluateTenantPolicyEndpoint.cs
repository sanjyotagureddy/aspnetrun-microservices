using Auth.Api.Infrastructure.Security;

namespace Auth.Api.Features.Auth.Internal;

internal sealed class EvaluateTenantPolicyEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapAuthV1()
            .MapPost("/internal/policies/evaluate", HandleAsync)
            .WithName(AuthRouteNames.EvaluateTenantPolicy)
            .RequireAuthorization(AuthPolicyNames.UserOnly);
    }

    private static async Task<IResult> HandleAsync(
        PolicyEvaluationRequest request,
        HttpContext httpContext,
        ITenantAuthorizationService authorizationService,
        CancellationToken cancellationToken)
    {
        PolicyDecision decision = request.Policy switch
        {
            AuthPolicyNames.TenantMemberPolicy => await authorizationService.EvaluateTenantMemberAsync(httpContext.User, request.TargetTenantId, cancellationToken),
            AuthPolicyNames.CatalogWritePolicy => await authorizationService.EvaluateCatalogWriteAsync(httpContext.User, request.TargetTenantId, cancellationToken),
            AuthPolicyNames.CheckoutPolicy => await authorizationService.EvaluateCheckoutAsync(httpContext.User, request.TargetTenantId, cancellationToken),
            AuthPolicyNames.PlatformAdminPolicy => await authorizationService.EvaluatePlatformAdminAsync(httpContext.User, request.TargetTenantId, request.Audit, cancellationToken),
            _ => PolicyDecision.Deny($"Unsupported policy: {request.Policy}")
        };

        PolicyEvaluationResponse response = new(request.Policy, request.TargetTenantId, decision.Allowed, decision.Reason);
        return TypedResults.Ok(response);
    }
}
