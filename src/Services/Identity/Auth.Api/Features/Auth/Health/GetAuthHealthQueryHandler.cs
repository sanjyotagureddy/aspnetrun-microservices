namespace Auth.Api.Features.Auth.Health;

internal sealed class GetAuthHealthQueryHandler(TimeProvider timeProvider) : IRequestHandler<GetAuthHealthQuery, Result<AuthHealthResponse>>
{
    public Task<Result<AuthHealthResponse>> Handle(GetAuthHealthQuery request, CancellationToken cancellationToken)
    {
        AuthHealthResponse response = new("auth-api", "healthy", timeProvider.GetUtcNow().UtcDateTime);
        return Task.FromResult(Result<AuthHealthResponse>.Success(response));
    }
}
