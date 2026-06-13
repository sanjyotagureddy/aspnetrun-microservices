using Auth.Api.Infrastructure.Configuration;
using Auth.Api.Infrastructure.Persistence;
using Auth.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Features.Auth.Internal;

[EndpointScope(EndpointScope.Development)]
internal sealed class BootstrapMembershipEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapAuthV1()
            .MapPost("/internal/bootstrap/memberships", HandleAsync)
            .WithName(AuthRouteNames.BootstrapMembership)
            .AllowAnonymous();
    }

    private static async Task<IResult> HandleAsync(
        BootstrapMembershipRequest request,
        IOptions<DevBootstrapOptions> bootstrapOptions,
        IHostEnvironment environment,
        AuthDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        if (!environment.IsDevelopment())
        {
            return TypedResults.NotFound();
        }

        DevBootstrapOptions options = bootstrapOptions.Value;

        if (!options.Enabled)
        {
            return TypedResults.Forbid();
        }

        if (!string.Equals(options.SharedSecret, request.Secret, StringComparison.Ordinal))
        {
            return TypedResults.Forbid();
        }

        string normalizedRole = request.Role.Trim();
        bool isKnownRole = TenantRoleNames.All.Contains(normalizedRole, StringComparer.Ordinal);

        if (!isKnownRole)
        {
            return TypedResults.BadRequest(new { error = $"Unsupported role: {request.Role}" });
        }

        UserTenantMembership? existing = await dbContext.UserTenantMemberships
            .SingleOrDefaultAsync(
                x => x.Subject == request.Subject && x.TenantId == request.TenantId && x.Role == normalizedRole,
                cancellationToken);

        DateTime createdUtc;
        string status;

        if (existing is null)
        {
            createdUtc = timeProvider.GetUtcNow().UtcDateTime;

            UserTenantMembership membership = new()
            {
                Subject = request.Subject,
                TenantId = request.TenantId,
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

        BootstrapMembershipResponse response = new(
            request.Subject,
            request.TenantId,
            normalizedRole,
            status,
            createdUtc);

        return status == "created"
            ? TypedResults.Created($"/api/v1/auth/internal/bootstrap/memberships", response)
            : TypedResults.Ok(response);
    }
}
