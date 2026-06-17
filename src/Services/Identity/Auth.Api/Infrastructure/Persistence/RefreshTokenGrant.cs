namespace Auth.Api.Infrastructure.Persistence;

internal sealed class RefreshTokenGrant
{
    public Guid Id { get; set; }
    public Guid FamilyId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public string? ParentTokenHash { get; set; }
    public string? Subject { get; set; }
    public DateTime IssuedUtc { get; set; }
    public DateTime ExpiresUtc { get; set; }
    public DateTime? ConsumedUtc { get; set; }
    public DateTime? RevokedUtc { get; set; }
    public string? RevocationReason { get; set; }
}
