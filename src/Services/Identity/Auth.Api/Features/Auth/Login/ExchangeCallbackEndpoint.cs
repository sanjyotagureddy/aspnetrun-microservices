using Auth.Api.Infrastructure.Persistence;
using Auth.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Features.Auth.Login;

internal sealed class ExchangeCallbackEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapAuthV1();

        group.MapPost("/callback/exchange", HandleAsync)
            .WithName(AuthRouteNames.ExchangeCallback);
    }

    private static async Task<IResult> HandleAsync(ExchangeCallbackRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        Result<ExchangeCallbackResponse> result = await mediator.Send(new ExchangeCallbackCommand(request), cancellationToken);
        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.BadRequest(new { error = result.Error });
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
}
