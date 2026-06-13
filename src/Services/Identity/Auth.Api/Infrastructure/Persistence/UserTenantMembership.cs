namespace Auth.Api.Infrastructure.Persistence;

internal sealed class UserTenantMembership
{
    public string Subject { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
}
