using Auth.Api.Infrastructure.Persistence;
using Auth.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Features.Auth.Internal;

internal sealed class AssignTenantRoleEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapAuthV1()
            .MapPost("/internal/tenants/{tenantId}/memberships", HandleAsync)
            .WithName(AuthRouteNames.AssignTenantRole)
            .RequireAuthorization(AuthPolicyNames.UserOnly);
    }

    private static async Task<Results<Created<AssignTenantRoleResponse>, Ok<AssignTenantRoleResponse>, ProblemHttpResult>> HandleAsync(
        string tenantId,
        AssignTenantRoleRequest request,
        HttpContext httpContext,
        ITenantAuthorizationService authorizationService,
        AuthDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        string normalizedRole = request.Role.Trim();
        bool isKnownRole = TenantRoleNames.All.Contains(normalizedRole, StringComparer.Ordinal);

        if (!isKnownRole)
        {
            return TypedResults.Problem(
                detail: $"Unsupported role: {request.Role}",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid tenant role assignment request");
        }

        PolicyDecision adminDecision = await authorizationService.EvaluatePlatformAdminAsync(
            httpContext.User,
            tenantId,
            request.Audit,
            cancellationToken);

        if (!adminDecision.Allowed)
        {
            return TypedResults.Problem(
                detail: adminDecision.Reason,
                statusCode: StatusCodes.Status403Forbidden,
                title: "Access denied");
        }

        UserTenantMembership? existing = await dbContext.UserTenantMemberships
            .SingleOrDefaultAsync(
                x => x.Subject == request.Subject && x.TenantId == tenantId && x.Role == normalizedRole,
                cancellationToken);

        DateTime createdUtc;
        string status;

        if (existing is null)
        {
            createdUtc = timeProvider.GetUtcNow().UtcDateTime;
            UserTenantMembership membership = new()
            {
                Subject = request.Subject,
                TenantId = tenantId,
                Role = normalizedRole,
                CreatedUtc = createdUtc
            };

            dbContext.UserTenantMemberships.Add(membership);
            await dbContext.SaveChangesAsync(cancellationToken);
            status = "created";
        }
        else
        {
            createdUtc = existing.CreatedUtc;
            status = "exists";
        }

        AssignTenantRoleResponse response = new(
            request.Subject,
            tenantId,
            normalizedRole,
            status,
            createdUtc);

        return status == "created"
            ? TypedResults.Created($"/api/v1/auth/internal/tenants/{tenantId}/memberships", response)
            : TypedResults.Ok(response);
    }
}
