using Auth.Api.Infrastructure.Persistence;
using Auth.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Auth.Api.Features.Auth.Tokens;

internal sealed class LogoutEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapAuthV1();

        group.MapPost("/logout", HandleAsync)
            .WithName(AuthRouteNames.Logout)
            .RequireAuthorization(AuthPolicyNames.UserOnly);
    }

    private static async Task<IResult> HandleAsync(LogoutRequest request, HttpContext httpContext, IMediator mediator, CancellationToken cancellationToken)
    {
        Result<LogoutResponse> result = await mediator.Send(new LogoutCommand(request, httpContext.User), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.BadRequest(new { error = result.Error });
    }
}

internal sealed record LogoutCommand(LogoutRequest Request, ClaimsPrincipal User) : IRequest<Result<LogoutResponse>>;

internal sealed class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator()
    {
        RuleFor(x => x.Request.SessionId)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.SessionId));

        RuleFor(x => x.Request.AccessTokenJti)
            .MaximumLength(200)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.AccessTokenJti));

        RuleFor(x => x.Request)
            .Must(x => !string.IsNullOrWhiteSpace(x.RefreshToken) || !string.IsNullOrWhiteSpace(x.AccessTokenJti) || !string.IsNullOrWhiteSpace(x.SessionId))
            .WithMessage("At least one of refreshToken, accessTokenJti, or sessionId must be provided.");
    }
}

internal sealed class LogoutCommandHandler(
    AuthDbContext dbContext,
    TimeProvider timeProvider) : IRequestHandler<LogoutCommand, Result<LogoutResponse>>
{
    public async Task<Result<LogoutResponse>> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;
        string? subject = command.User.FindFirstValue("sub");
        string? refreshTokenHash = string.IsNullOrWhiteSpace(command.Request.RefreshToken)
            ? null
            : RefreshTokenCrypto.Hash(command.Request.RefreshToken);

        if (!string.IsNullOrWhiteSpace(refreshTokenHash))
        {
            RefreshTokenGrant? grant = await dbContext.RefreshTokenGrants
                .SingleOrDefaultAsync(x => x.TokenHash == refreshTokenHash, cancellationToken);

            if (grant is not null)
            {
                List<RefreshTokenGrant> family = await dbContext.RefreshTokenGrants
                    .Where(x => x.FamilyId == grant.FamilyId && x.RevokedUtc == null)
                    .ToListAsync(cancellationToken);

                foreach (RefreshTokenGrant familyGrant in family)
                {
                    familyGrant.RevokedUtc = utcNow;
                    familyGrant.RevocationReason = "logout";
                }
            }
        }

        TokenOperation operation = new()
        {
            Id = Guid.NewGuid(),
            OperationType = "logout",
            Subject = subject,
            SessionId = command.Request.SessionId,
            AccessTokenJti = command.Request.AccessTokenJti,
            RefreshTokenHash = refreshTokenHash,
            CreatedUtc = utcNow
        };

        dbContext.TokenOperations.Add(operation);
        await dbContext.SaveChangesAsync(cancellationToken);

        LogoutResponse response = new(
            operation.Id,
            "recorded",
            "Logout request recorded. Authority session revocation integration is pending.");

        return Result<LogoutResponse>.Success(response);
    }
}
