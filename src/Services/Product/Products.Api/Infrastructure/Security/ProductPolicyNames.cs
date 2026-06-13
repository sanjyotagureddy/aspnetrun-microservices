namespace Products.Api.Infrastructure.Security;

internal static class ProductPolicyNames
{
    public const string UserPrincipalOnly = "Products_UserPrincipalOnly";
    public const string TenantReadPolicy = "Products_TenantReadPolicy";
    public const string CatalogWritePolicy = "Products_CatalogWritePolicy";
}

internal static class ProductRoleNames
{
    public const string TenantAdmin = "tenant_admin";
    public const string CatalogManager = "catalog_manager";
    public const string Buyer = "buyer";
    public const string PlatformAdmin = "platform_admin";
}
