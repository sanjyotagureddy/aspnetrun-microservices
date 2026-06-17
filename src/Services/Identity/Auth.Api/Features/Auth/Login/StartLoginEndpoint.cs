using Auth.Api.Infrastructure.Configuration;
using Auth.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Features.Auth.Login;

internal sealed class StartLoginEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapAuthV1();

        group.MapPost("/login/start", HandleAsync)
            .WithName(AuthRouteNames.StartLogin);
    }

    private static async Task<Microsoft.AspNetCore.Http.HttpResults.Results<Microsoft.AspNetCore.Http.HttpResults.Ok<StartLoginResponse>, Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>> HandleAsync(StartLoginRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        Result<StartLoginResponse> result = await mediator.Send(new StartLoginCommand(request), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid login request");
    }
}

internal sealed record StartLoginCommand(StartLoginRequest Request) : IRequest<Result<StartLoginResponse>>;

internal sealed class StartLoginCommandValidator : AbstractValidator<StartLoginCommand>
{
    public StartLoginCommandValidator()
    {
        RuleFor(x => x.Request.RedirectUri)
            .NotEmpty()
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _));

        RuleFor(x => x.Request.CodeChallenge)
            .NotEmpty()
            .MinimumLength(43)
            .MaximumLength(128);

        RuleFor(x => x.Request.CodeChallengeMethod)
            .NotEmpty()
            .Must(method => method is "S256");

        RuleFor(x => x.Request.ClientId)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.ClientId));

        RuleFor(x => x.Request.State)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.State));

        RuleFor(x => x.Request.Nonce)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.Nonce));

        RuleFor(x => x.Request.TenantHint)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.TenantHint));
    }
}

internal sealed class StartLoginCommandHandler(
    AuthDbContext dbContext,
    IOptions<AuthOptions> authOptions,
    TimeProvider timeProvider) : IRequestHandler<StartLoginCommand, Result<StartLoginResponse>>
{
    public async Task<Result<StartLoginResponse>> Handle(StartLoginCommand command, CancellationToken cancellationToken)
    {
        AuthOptions options = authOptions.Value;
        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;
        string state = command.Request.State ?? Guid.NewGuid().ToString("N");
        string nonce = command.Request.Nonce ?? Guid.NewGuid().ToString("N");
        DateTime expiresUtc = utcNow.AddMinutes(10);
        string clientId = string.IsNullOrWhiteSpace(command.Request.ClientId)
            ? options.WebClientId
            : command.Request.ClientId;

        PkceClientOptions? configuredClient = options.PkceClients
            .FirstOrDefault(client => string.Equals(client.ClientId, clientId, StringComparison.Ordinal));

        if (configuredClient is null)
        {
            return Result<StartLoginResponse>.Failure($"Unknown PKCE client '{clientId}'.");
        }

        bool redirectUriAllowed = configuredClient.RedirectUris.Any(uri =>
            string.Equals(uri, command.Request.RedirectUri, StringComparison.OrdinalIgnoreCase));

        if (!redirectUriAllowed)
        {
            return Result<StartLoginResponse>.Failure("Redirect URI is not allowed for the selected PKCE client.");
        }

        bool stateExists = await dbContext.LoginTransactions.AnyAsync(x => x.State == state, cancellationToken);
        if (stateExists)
        {
            return Result<StartLoginResponse>.Failure("State value already exists. Retry login start with a new state.");
        }

        LoginTransaction transaction = new()
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            State = state,
            Nonce = nonce,
            RedirectUri = command.Request.RedirectUri,
            CodeChallenge = command.Request.CodeChallenge,
            CodeChallengeMethod = command.Request.CodeChallengeMethod,
            TenantHint = command.Request.TenantHint,
            CreatedUtc = utcNow,
            ExpiresUtc = expiresUtc
        };

        dbContext.LoginTransactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        string authority = options.Authority.TrimEnd('/');
        string scope = string.IsNullOrWhiteSpace(configuredClient.Scope)
            ? options.WebClientScope
            : configuredClient.Scope;

        Dictionary<string, string?> query = new()
        {
            ["client_id"] = clientId,
            ["response_type"] = "code",
            ["scope"] = scope,
            ["redirect_uri"] = command.Request.RedirectUri,
            ["code_challenge"] = command.Request.CodeChallenge,
            ["code_challenge_method"] = command.Request.CodeChallengeMethod,
            ["state"] = state,
            ["nonce"] = nonce
        };

        if (!string.IsNullOrWhiteSpace(command.Request.TenantHint))
        {
            query["tenant_hint"] = command.Request.TenantHint;
        }

        string authorizationEndpoint = QueryHelpers.AddQueryString($"{authority}/protocol/openid-connect/auth", query);
        StartLoginResponse response = new(authorizationEndpoint, state, nonce, expiresUtc);
        return Result<StartLoginResponse>.Success(response);
    }
}
