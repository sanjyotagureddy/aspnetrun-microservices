namespace Auth.Api.Contracts;

public sealed record AuthHealthResponse(string Service, string Status, DateTime UtcNow);

public sealed record StartLoginRequest(
	string? ClientId,
	string RedirectUri,
	string CodeChallenge,
	string CodeChallengeMethod,
	string? State,
	string? Nonce,
	string? TenantHint);

public sealed record StartLoginResponse(string AuthorizationUrl, string State, string Nonce, DateTime ExpiresUtc);

public sealed record ExchangeCallbackRequest(string State, string Code, string CodeVerifier, string RedirectUri);

public sealed record ExchangeCallbackResponse(
	Guid TransactionId,
	string Status,
	string Message,
	string? RefreshToken,
	DateTime? RefreshTokenExpiresUtc);

public sealed record RefreshTokenRequest(string RefreshToken, string? IdempotencyKey);

public sealed record RefreshTokenResponse(
	Guid OperationId,
	string Status,
	string Message,
	string? RefreshToken,
	DateTime? RefreshTokenExpiresUtc);

public sealed record LogoutRequest(string? SessionId, string? RefreshToken, string? AccessTokenJti);

public sealed record LogoutResponse(Guid OperationId, string Status, string Message);

public sealed record WorkloadValidationResponse(
	string ClientId,
	IReadOnlyCollection<string> Scopes,
	IReadOnlyCollection<string> Audiences,
	DateTimeOffset? IssuedAtUtc,
	DateTimeOffset? ExpiresAtUtc);

public sealed record PolicyEvaluationAuditRequest(
	string? ReasonCode,
	string? ChangeTicketId,
	string? ApprovedBy,
	DateTimeOffset? ApprovedAtUtc);

public sealed record PolicyEvaluationRequest(
	string Policy,
	string TargetTenantId,
	PolicyEvaluationAuditRequest? Audit);

public sealed record PolicyEvaluationResponse(
	string Policy,
	string TargetTenantId,
	bool Allowed,
	string Reason);

public sealed record AssignTenantRoleRequest(
	string Subject,
	string Role,
	PolicyEvaluationAuditRequest Audit);

public sealed record AssignTenantRoleResponse(
	string Subject,
	string TenantId,
	string Role,
	string Status,
	DateTime CreatedUtc);

public sealed record BootstrapMembershipRequest(
	string Subject,
	string TenantId,
	string Role,
	string Secret);

public sealed record BootstrapMembershipResponse(
	string Subject,
	string TenantId,
	string Role,
	string Status,
	DateTime CreatedUtc);

public sealed record TenantMembershipResponse(string TenantId, IReadOnlyCollection<string> Roles);

public sealed record UserProfileResponse(
	string Subject,
	string? Name,
	string? Email,
	IReadOnlyCollection<TenantMembershipResponse> Memberships);
