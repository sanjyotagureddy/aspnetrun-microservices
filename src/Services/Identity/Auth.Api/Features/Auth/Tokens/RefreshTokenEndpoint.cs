using Auth.Api.Infrastructure.Persistence;
using Auth.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Features.Auth.Tokens;

internal sealed class RefreshTokenEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapAuthV1();

        group.MapPost("/token/refresh", HandleAsync)
            .WithName(AuthRouteNames.RefreshToken);
    }

    private static async Task<IResult> HandleAsync(RefreshTokenRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        Result<RefreshTokenResponse> result = await mediator.Send(new RefreshTokenCommand(request), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.BadRequest(new { error = result.Error });
    }
}

internal sealed record RefreshTokenCommand(RefreshTokenRequest Request) : IRequest<Result<RefreshTokenResponse>>;

internal sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.Request.RefreshToken).NotEmpty();

        RuleFor(x => x.Request.IdempotencyKey)
            .MaximumLength(100)
            .When(x => !string.IsNullOrWhiteSpace(x.Request.IdempotencyKey));
    }
}

internal sealed class RefreshTokenCommandHandler(
    AuthDbContext dbContext,
    TimeProvider timeProvider) : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;
        string refreshTokenHash = RefreshTokenCrypto.Hash(command.Request.RefreshToken);

        if (!string.IsNullOrWhiteSpace(command.Request.IdempotencyKey))
        {
            TokenOperation? existing = await dbContext.TokenOperations
                .SingleOrDefaultAsync(
                    x => x.OperationType == "refresh" && x.IdempotencyKey == command.Request.IdempotencyKey,
                    cancellationToken);

            if (existing is not null)
            {
                RefreshTokenResponse idempotentResponse = new(
                    existing.Id,
                    "accepted",
                    "Refresh request already recorded for this idempotency key.",
                    null,
                    null);

                return Result<RefreshTokenResponse>.Success(idempotentResponse);
            }
        }

        RefreshTokenGrant? currentGrant = await dbContext.RefreshTokenGrants
            .SingleOrDefaultAsync(x => x.TokenHash == refreshTokenHash, cancellationToken);

        if (currentGrant is null)
        {
            return Result<RefreshTokenResponse>.Failure("Invalid refresh token.");
        }

        if (currentGrant.RevokedUtc is not null)
        {
            return Result<RefreshTokenResponse>.Failure("Refresh token has been revoked.");
        }

        if (currentGrant.ExpiresUtc <= utcNow)
        {
            currentGrant.RevokedUtc = utcNow;
            currentGrant.RevocationReason = "expired";
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result<RefreshTokenResponse>.Failure("Refresh token has expired.");
        }

        if (currentGrant.ConsumedUtc is not null)
        {
            List<RefreshTokenGrant> family = await dbContext.RefreshTokenGrants
                .Where(x => x.FamilyId == currentGrant.FamilyId && x.RevokedUtc == null)
                .ToListAsync(cancellationToken);

            foreach (RefreshTokenGrant grant in family)
            {
                grant.RevokedUtc = utcNow;
                grant.RevocationReason = "reuse-detected";
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            return Result<RefreshTokenResponse>.Failure("Refresh token reuse detected. Token family has been revoked.");
        }

        string newRefreshToken = RefreshTokenCrypto.GenerateToken();
        string newRefreshTokenHash = RefreshTokenCrypto.Hash(newRefreshToken);
        DateTime newExpiresUtc = utcNow.AddDays(14);

        currentGrant.ConsumedUtc = utcNow;

        RefreshTokenGrant rotatedGrant = new()
        {
            Id = Guid.NewGuid(),
            FamilyId = currentGrant.FamilyId,
            TokenHash = newRefreshTokenHash,
            ParentTokenHash = currentGrant.TokenHash,
            Subject = currentGrant.Subject,
            IssuedUtc = utcNow,
            ExpiresUtc = newExpiresUtc
        };

        dbContext.RefreshTokenGrants.Add(rotatedGrant);

        TokenOperation operation = new()
        {
            Id = Guid.NewGuid(),
            OperationType = "refresh",
            IdempotencyKey = command.Request.IdempotencyKey,
            RefreshTokenHash = refreshTokenHash,
            CreatedUtc = utcNow
        };

        dbContext.TokenOperations.Add(operation);
        await dbContext.SaveChangesAsync(cancellationToken);

        RefreshTokenResponse response = new(
            operation.Id,
            "rotated",
            "Refresh token rotated successfully.",
            newRefreshToken,
            newExpiresUtc);

        return Result<RefreshTokenResponse>.Success(response);
    }
}
