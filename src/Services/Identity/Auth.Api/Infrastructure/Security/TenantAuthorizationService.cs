using Auth.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Auth.Api.Infrastructure.Security;

internal interface ITenantAuthorizationService
{
    Task<PolicyDecision> EvaluateTenantMemberAsync(ClaimsPrincipal principal, string targetTenantId, CancellationToken cancellationToken);
    Task<PolicyDecision> EvaluateCatalogWriteAsync(ClaimsPrincipal principal, string targetTenantId, CancellationToken cancellationToken);
    Task<PolicyDecision> EvaluateCheckoutAsync(ClaimsPrincipal principal, string targetTenantId, CancellationToken cancellationToken);
    Task<PolicyDecision> EvaluatePlatformAdminAsync(ClaimsPrincipal principal, string targetTenantId, PolicyEvaluationAuditRequest? audit, CancellationToken cancellationToken);
}

internal sealed class TenantAuthorizationService(
    AuthDbContext dbContext)
    : ITenantAuthorizationService
{
    private static readonly HashSet<string> CatalogWriteRoles = new(StringComparer.Ordinal)
    {
        TenantRoleNames.TenantAdmin,
        TenantRoleNames.CatalogManager
    };

    private static readonly HashSet<string> CheckoutRoles = new(StringComparer.Ordinal)
    {
        TenantRoleNames.Buyer,
        TenantRoleNames.TenantAdmin
    };

    public async Task<PolicyDecision> EvaluateTenantMemberAsync(ClaimsPrincipal principal, string targetTenantId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetTenantId))
        {
            return PolicyDecision.Deny("Target tenant is required.");
        }

        string? subject = principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(subject))
        {
            return PolicyDecision.Deny("Missing subject claim.");
        }

        string? tokenTenantId = principal.FindFirstValue("tenant_id");
        if (string.IsNullOrWhiteSpace(tokenTenantId))
        {
            return PolicyDecision.Deny("tenant_id claim is required for tenant-scoped policies.");
        }

        if (!string.Equals(tokenTenantId, targetTenantId, StringComparison.Ordinal))
        {
            return PolicyDecision.Deny("Cross-tenant access is denied unless explicitly evaluated via PlatformAdminPolicy.");
        }

        HashSet<string> effectiveRoles = await GetEffectiveRolesAsync(principal, subject, targetTenantId, cancellationToken);
        bool isTenantMember = effectiveRoles.Overlaps(TenantRoleNames.All);

        return isTenantMember
            ? PolicyDecision.Allow()
            : PolicyDecision.Deny("No tenant membership role found for target tenant.");
    }

    public async Task<PolicyDecision> EvaluateCatalogWriteAsync(ClaimsPrincipal principal, string targetTenantId, CancellationToken cancellationToken)
    {
        PolicyDecision membership = await EvaluateTenantMemberAsync(principal, targetTenantId, cancellationToken);
        if (!membership.Allowed)
        {
            return membership;
        }

        HashSet<string> scopes = GetScopes(principal);
        if (!scopes.Contains("products.write"))
        {
            return PolicyDecision.Deny("Missing required scope: products.write.");
        }

        string subject = principal.FindFirstValue("sub")!;
        HashSet<string> effectiveRoles = await GetEffectiveRolesAsync(principal, subject, targetTenantId, cancellationToken);

        return effectiveRoles.Overlaps(CatalogWriteRoles)
            ? PolicyDecision.Allow()
            : PolicyDecision.Deny("Missing required role: tenant_admin or catalog_manager.");
    }

    public async Task<PolicyDecision> EvaluateCheckoutAsync(ClaimsPrincipal principal, string targetTenantId, CancellationToken cancellationToken)
    {
        PolicyDecision membership = await EvaluateTenantMemberAsync(principal, targetTenantId, cancellationToken);
        if (!membership.Allowed)
        {
            return membership;
        }

        HashSet<string> scopes = GetScopes(principal);
        if (!scopes.Contains("checkout.create"))
        {
            return PolicyDecision.Deny("Missing required scope: checkout.create.");
        }

        string subject = principal.FindFirstValue("sub")!;
        HashSet<string> effectiveRoles = await GetEffectiveRolesAsync(principal, subject, targetTenantId, cancellationToken);

        return effectiveRoles.Overlaps(CheckoutRoles)
            ? PolicyDecision.Allow()
            : PolicyDecision.Deny("Missing required role: buyer or tenant_admin.");
    }

    public async Task<PolicyDecision> EvaluatePlatformAdminAsync(ClaimsPrincipal principal, string targetTenantId, PolicyEvaluationAuditRequest? audit, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(targetTenantId))
        {
            return PolicyDecision.Deny("Target tenant is required.");
        }

        string? subject = principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(subject))
        {
            return PolicyDecision.Deny("Missing subject claim.");
        }

        if (audit is null
            || string.IsNullOrWhiteSpace(audit.ReasonCode)
            || string.IsNullOrWhiteSpace(audit.ChangeTicketId)
            || string.IsNullOrWhiteSpace(audit.ApprovedBy)
            || audit.ApprovedAtUtc is null)
        {
            return PolicyDecision.Deny("PlatformAdminPolicy requires audit fields: reason_code, change_ticket_id, approved_by, approved_at_utc.");
        }

        HashSet<string> platformAdminRoles = await GetPlatformAdminRolesAsync(principal, subject, cancellationToken);
        bool isPlatformAdmin = platformAdminRoles.Contains(TenantRoleNames.PlatformAdmin);

        return isPlatformAdmin
            ? PolicyDecision.Allow()
            : PolicyDecision.Deny("Missing required role: platform_admin.");
    }

    private async Task<HashSet<string>> GetEffectiveRolesAsync(ClaimsPrincipal principal, string subject, string tenantId, CancellationToken cancellationToken)
    {
        HashSet<string> roles = GetRolesFromClaims(principal);

        string[] dbRoles = await dbContext.UserTenantMemberships
            .Where(x => x.Subject == subject && x.TenantId == tenantId)
            .Select(x => x.Role)
            .ToArrayAsync(cancellationToken);

        roles.UnionWith(dbRoles);
        return roles;
    }

    private async Task<HashSet<string>> GetPlatformAdminRolesAsync(ClaimsPrincipal principal, string subject, CancellationToken cancellationToken)
    {
        HashSet<string> roles = GetRolesFromClaims(principal);

        bool dbPlatformAdmin = await dbContext.UserTenantMemberships
            .AnyAsync(
                x => x.Subject == subject && x.Role == TenantRoleNames.PlatformAdmin,
                cancellationToken);

        if (dbPlatformAdmin)
        {
            roles.Add(TenantRoleNames.PlatformAdmin);
        }

        return roles;
    }

    private static HashSet<string> GetScopes(ClaimsPrincipal principal)
    {
        IEnumerable<string> scopeValues = principal.FindAll("scope")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return new HashSet<string>(scopeValues, StringComparer.Ordinal);
    }

    private static HashSet<string> GetRolesFromClaims(ClaimsPrincipal principal)
    {
        IEnumerable<string> roleValues = principal.FindAll("role")
            .Concat(principal.FindAll(ClaimTypes.Role))
            .Select(claim => claim.Value);

        return new HashSet<string>(roleValues, StringComparer.Ordinal);
    }
}

internal sealed record PolicyDecision(bool Allowed, string Reason)
{
    public static PolicyDecision Allow() => new(true, "allowed");
    public static PolicyDecision Deny(string reason) => new(false, reason);
}
