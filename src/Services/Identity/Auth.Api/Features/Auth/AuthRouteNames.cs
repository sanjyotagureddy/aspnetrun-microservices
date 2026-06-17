namespace Auth.Api.Features.Auth;

internal static class AuthRouteNames
{
    public const string GetHealth = "Auth_GetHealth";
    public const string StartLogin = "Auth_StartLogin";
    public const string ExchangeCallback = "Auth_ExchangeCallback";
    public const string RefreshToken = "Auth_RefreshToken";
    public const string Logout = "Auth_Logout";
    public const string GetMyProfile = "Auth_GetMyProfile";
    public const string ValidateWorkload = "Auth_ValidateWorkload";
    public const string EvaluateTenantPolicy = "Auth_EvaluateTenantPolicy";
    public const string AssignTenantRole = "Auth_AssignTenantRole";
    public const string BootstrapMembership = "Auth_BootstrapMembership";
}
