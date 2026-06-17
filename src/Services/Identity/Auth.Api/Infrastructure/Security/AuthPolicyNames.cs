namespace Auth.Api.Infrastructure.Security;

internal static class AuthPolicyNames
{
    public const string WorkloadOnly = "WorkloadOnly";
    public const string UserOnly = "UserOnly";
    public const string TenantMemberPolicy = "TenantMemberPolicy";
    public const string CatalogWritePolicy = "CatalogWritePolicy";
    public const string CheckoutPolicy = "CheckoutPolicy";
    public const string PlatformAdminPolicy = "PlatformAdminPolicy";
}

internal static class TenantRoleNames
{
    public const string TenantAdmin = "tenant_admin";
    public const string CatalogManager = "catalog_manager";
    public const string Buyer = "buyer";
    public const string PlatformAdmin = "platform_admin";

    public static readonly string[] All =
    [
        TenantAdmin,
        CatalogManager,
        Buyer,
        PlatformAdmin
    ];
}
