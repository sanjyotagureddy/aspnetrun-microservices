namespace Auth.Api.Features.Auth.Health;

internal sealed record GetAuthHealthQuery : IRequest<Result<AuthHealthResponse>>;
