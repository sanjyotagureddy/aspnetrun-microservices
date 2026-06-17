using Auth.Api.Infrastructure.Persistence;
using Auth.Api.Infrastructure.Security;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Auth.Api.Features.Auth.Login;

internal sealed class ExchangeCallbackEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapAuthV1();

        group.MapPost("/callback/exchange", HandleAsync)
            .WithName(AuthRouteNames.ExchangeCallback);
    }

    private static async Task<Microsoft.AspNetCore.Http.HttpResults.Results<Microsoft.AspNetCore.Http.HttpResults.Ok<ExchangeCallbackResponse>, Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>> HandleAsync(ExchangeCallbackRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        Result<ExchangeCallbackResponse> result = await mediator.Send(new ExchangeCallbackCommand(request), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid callback exchange request");
    }
}

internal sealed record ExchangeCallbackCommand(ExchangeCallbackRequest Request) : IRequest<Result<ExchangeCallbackResponse>>;

internal sealed class ExchangeCallbackCommandValidator : AbstractValidator<ExchangeCallbackCommand>
{
    public ExchangeCallbackCommandValidator()
    {
        RuleFor(x => x.Request.State).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.Code).NotEmpty();
        RuleFor(x => x.Request.CodeVerifier).NotEmpty().MinimumLength(43).MaximumLength(128);
        RuleFor(x => x.Request.ClientId).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.RedirectUri)
            .NotEmpty()
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _));
    }
}

internal sealed class ExchangeCallbackCommandHandler(
    AuthDbContext dbContext,
    TimeProvider timeProvider) : IRequestHandler<ExchangeCallbackCommand, Result<ExchangeCallbackResponse>>
{
    public async Task<Result<ExchangeCallbackResponse>> Handle(ExchangeCallbackCommand command, CancellationToken cancellationToken)
    {
        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;

        LoginTransaction? transaction = await dbContext.LoginTransactions
            .SingleOrDefaultAsync(x => x.State == command.Request.State, cancellationToken);

        if (transaction is null)
        {
            return Result<ExchangeCallbackResponse>.Failure("Unknown login state.");
        }

        if (transaction.ExchangeCompletedUtc is not null)
        {
            return Result<ExchangeCallbackResponse>.Failure("Login state has already been exchanged.");
        }

        if (transaction.ExpiresUtc < utcNow)
        {
            return Result<ExchangeCallbackResponse>.Failure("Login state has expired.");
        }

        if (!string.Equals(transaction.RedirectUri, command.Request.RedirectUri, StringComparison.OrdinalIgnoreCase))
        {
            return Result<ExchangeCallbackResponse>.Failure("Redirect URI does not match initial login request.");
        }

        if (!string.Equals(transaction.ClientId, command.Request.ClientId, StringComparison.Ordinal))
        {
            return Result<ExchangeCallbackResponse>.Failure("Client ID does not match initial login request.");
        }

        if (!string.Equals(transaction.CodeChallengeMethod, "S256", StringComparison.Ordinal))
        {
            return Result<ExchangeCallbackResponse>.Failure("Unsupported PKCE code_challenge_method for this login transaction.");
        }

        string computedChallenge = ComputeS256CodeChallenge(command.Request.CodeVerifier);
        if (!FixedTimeEquals(computedChallenge, transaction.CodeChallenge))
        {
            return Result<ExchangeCallbackResponse>.Failure("PKCE code verifier is invalid.");
        }

        transaction.AuthorizationCode = command.Request.Code;
        transaction.ExchangeCompletedUtc = utcNow;

        string refreshToken = RefreshTokenCrypto.GenerateToken();
        DateTime refreshTokenExpiresUtc = utcNow.AddDays(14);

        RefreshTokenGrant grant = new()
        {
            Id = Guid.NewGuid(),
            FamilyId = Guid.NewGuid(),
            TokenHash = RefreshTokenCrypto.Hash(refreshToken),
            Subject = null,
            IssuedUtc = utcNow,
            ExpiresUtc = refreshTokenExpiresUtc
        };

        dbContext.RefreshTokenGrants.Add(grant);

        await dbContext.SaveChangesAsync(cancellationToken);

        ExchangeCallbackResponse response = new(
            transaction.Id,
            "issued",
            "Authorization code captured and refresh token issued for rotation flow.",
            refreshToken,
            refreshTokenExpiresUtc);

        return Result<ExchangeCallbackResponse>.Success(response);
    }

    private static string ComputeS256CodeChallenge(string codeVerifier)
    {
        byte[] verifierBytes = Encoding.ASCII.GetBytes(codeVerifier);
        byte[] hashed = SHA256.HashData(verifierBytes);
        return WebEncoders.Base64UrlEncode(hashed);
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        byte[] leftBytes = Encoding.UTF8.GetBytes(left);
        byte[] rightBytes = Encoding.UTF8.GetBytes(right);

        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
