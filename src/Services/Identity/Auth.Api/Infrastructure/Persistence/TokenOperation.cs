namespace Auth.Api.Infrastructure.Persistence;

internal sealed class TokenOperation
{
    public Guid Id { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? IdempotencyKey { get; set; }
    public string? RefreshTokenHash { get; set; }
    public string? AccessTokenJti { get; set; }
    public string? SessionId { get; set; }
    public DateTime CreatedUtc { get; set; }
}
