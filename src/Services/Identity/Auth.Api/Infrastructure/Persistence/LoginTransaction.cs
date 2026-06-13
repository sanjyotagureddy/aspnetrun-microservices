namespace Auth.Api.Infrastructure.Persistence;

internal sealed class LoginTransaction
{
    public Guid Id { get; set; }
    public string State { get; set; } = string.Empty;
    public string Nonce { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string CodeChallenge { get; set; } = string.Empty;
    public string CodeChallengeMethod { get; set; } = string.Empty;
    public string? TenantHint { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime ExpiresUtc { get; set; }
    public string? AuthorizationCode { get; set; }
    public DateTime? ExchangeCompletedUtc { get; set; }
}
