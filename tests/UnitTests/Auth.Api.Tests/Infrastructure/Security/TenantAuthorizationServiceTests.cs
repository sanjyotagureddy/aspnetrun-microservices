using System.Security.Claims;
using Auth.Api.Contracts;
using Auth.Api.Infrastructure.Persistence;
using Auth.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Tests.Infrastructure.Security;

public sealed class TenantAuthorizationServiceTests
{
    [Fact]
    public async Task EvaluateTenantMember_Should_Deny_When_TenantClaim_Is_Missing()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AuthDbContext dbContext = CreateDbContext();
        TenantAuthorizationService service = new(dbContext);

        ClaimsPrincipal principal = CreatePrincipal("user-1", tenantId: null, scope: "products.read");

        PolicyDecision result = await service.EvaluateTenantMemberAsync(principal, "tenant-a", cancellationToken);

        result.Allowed.Should().BeFalse();
        result.Reason.Should().Contain("tenant_id claim is required");
    }

    [Fact]
    public async Task EvaluateCatalogWrite_Should_Allow_When_Membership_And_Scope_Are_Valid()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AuthDbContext dbContext = CreateDbContext();

        dbContext.UserTenantMemberships.Add(new UserTenantMembership
        {
            Subject = "user-1",
            TenantId = "tenant-a",
            Role = TenantRoleNames.CatalogManager,
            CreatedUtc = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        TenantAuthorizationService service = new(dbContext);
        ClaimsPrincipal principal = CreatePrincipal("user-1", "tenant-a", "openid products.read products.write");

        PolicyDecision result = await service.EvaluateCatalogWriteAsync(principal, "tenant-a", cancellationToken);

        result.Allowed.Should().BeTrue();
    }

    [Fact]
    public async Task EvaluatePlatformAdmin_Should_Deny_When_Audit_Is_Missing()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AuthDbContext dbContext = CreateDbContext();
        TenantAuthorizationService service = new(dbContext);

        ClaimsPrincipal principal = CreatePrincipal("user-1", "tenant-a", "openid", roles: [TenantRoleNames.PlatformAdmin]);

        PolicyDecision result = await service.EvaluatePlatformAdminAsync(principal, "tenant-a", audit: null, cancellationToken);

        result.Allowed.Should().BeFalse();
        result.Reason.Should().Contain("requires audit fields");
    }

    [Fact]
    public async Task EvaluatePlatformAdmin_Should_Allow_When_PlatformAdmin_And_Audit_Are_Valid()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AuthDbContext dbContext = CreateDbContext();
        TenantAuthorizationService service = new(dbContext);

        ClaimsPrincipal principal = CreatePrincipal("user-1", "tenant-a", "openid", roles: [TenantRoleNames.PlatformAdmin]);
        PolicyEvaluationAuditRequest audit = new("RC", "CHG-1", "approver", DateTimeOffset.UtcNow);

        PolicyDecision result = await service.EvaluatePlatformAdminAsync(principal, "tenant-a", audit, cancellationToken);

        result.Allowed.Should().BeTrue();
    }

    private static ClaimsPrincipal CreatePrincipal(string subject, string? tenantId, string scope, string[]? roles = null)
    {
        List<Claim> claims =
        [
            new("sub", subject),
            new("scope", scope)
        ];

        if (!string.IsNullOrWhiteSpace(tenantId))
        {
            claims.Add(new Claim("tenant_id", tenantId));
        }

        if (roles is not null)
        {
            claims.AddRange(roles.Select(role => new Claim("role", role)));
        }

        ClaimsIdentity identity = new(claims, authenticationType: "test");
        return new ClaimsPrincipal(identity);
    }

    private static AuthDbContext CreateDbContext()
    {
        DbContextOptions<AuthDbContext> options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AuthDbContext(options);
    }
}
