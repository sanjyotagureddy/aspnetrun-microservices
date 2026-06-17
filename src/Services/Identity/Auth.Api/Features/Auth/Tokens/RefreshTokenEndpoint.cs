using Auth.Api.Infrastructure.Persistence;
using Auth.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Auth.Api.Features.Auth.Tokens;

internal sealed class RefreshTokenEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapAuthV1();

        group.MapPost("/token/refresh", HandleAsync)
            .WithName(AuthRouteNames.RefreshToken);
    }

    private static async Task<Microsoft.AspNetCore.Http.HttpResults.Results<Microsoft.AspNetCore.Http.HttpResults.Ok<RefreshTokenResponse>, Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>> HandleAsync(RefreshTokenRequest request, IMediator mediator, CancellationToken cancellationToken)
    {
        Result<RefreshTokenResponse> result = await mediator.Send(new RefreshTokenCommand(request), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid refresh token request");
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
        string? idempotencyKey = string.IsNullOrWhiteSpace(command.Request.IdempotencyKey)
            ? null
            : command.Request.IdempotencyKey;

        if (idempotencyKey is not null)
        {
            TokenOperation? existing = await dbContext.TokenOperations
                .SingleOrDefaultAsync(
                    x => x.OperationType == "refresh" && x.IdempotencyKey == idempotencyKey,
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

        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        int consumeRows = await dbContext.RefreshTokenGrants
            .Where(x => x.TokenHash == refreshTokenHash && x.ConsumedUtc == null && x.RevokedUtc == null && x.ExpiresUtc > utcNow)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(x => x.ConsumedUtc, utcNow),
                cancellationToken);

        RefreshTokenGrant? currentGrant = await dbContext.RefreshTokenGrants
            .SingleOrDefaultAsync(x => x.TokenHash == refreshTokenHash, cancellationToken);

        if (currentGrant is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result<RefreshTokenResponse>.Failure("Invalid refresh token.");
        }

        if (consumeRows == 0)
        {
            if (idempotencyKey is not null)
            {
                TokenOperation? existingOperation = await dbContext.TokenOperations
                    .SingleOrDefaultAsync(
                        x => x.OperationType == "refresh" && x.IdempotencyKey == idempotencyKey,
                        cancellationToken);

                if (existingOperation is not null)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    RefreshTokenResponse acceptedResponse = new(
                        existingOperation.Id,
                        "accepted",
                        "Refresh request already recorded for this idempotency key.",
                        null,
                        null);

                    return Result<RefreshTokenResponse>.Success(acceptedResponse);
                }
            }

            if (currentGrant.RevokedUtc is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Result<RefreshTokenResponse>.Failure("Refresh token has been revoked.");
            }

            if (currentGrant.ExpiresUtc <= utcNow)
            {
                await dbContext.RefreshTokenGrants
                    .Where(x => x.Id == currentGrant.Id && x.RevokedUtc == null)
                    .ExecuteUpdateAsync(
                        updates => updates
                            .SetProperty(x => x.RevokedUtc, utcNow)
                            .SetProperty(x => x.RevocationReason, "expired"),
                        cancellationToken);

                await transaction.CommitAsync(cancellationToken);
                return Result<RefreshTokenResponse>.Failure("Refresh token has expired.");
            }

            await dbContext.RefreshTokenGrants
                .Where(x => x.FamilyId == currentGrant.FamilyId && x.RevokedUtc == null)
                .ExecuteUpdateAsync(
                    updates => updates
                        .SetProperty(x => x.RevokedUtc, utcNow)
                        .SetProperty(x => x.RevocationReason, "reuse-detected"),
                    cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return Result<RefreshTokenResponse>.Failure("Refresh token reuse detected. Token family has been revoked.");
        }

        string newRefreshToken = RefreshTokenCrypto.GenerateToken();
        string newRefreshTokenHash = RefreshTokenCrypto.Hash(newRefreshToken);
        DateTime newExpiresUtc = utcNow.AddDays(14);

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
            IdempotencyKey = idempotencyKey,
            RefreshTokenHash = refreshTokenHash,
            CreatedUtc = utcNow
        };

        dbContext.TokenOperations.Add(operation);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException) when (idempotencyKey is not null)
        {
            await transaction.RollbackAsync(cancellationToken);

            TokenOperation? existing = await dbContext.TokenOperations
                .SingleOrDefaultAsync(
                    x => x.OperationType == "refresh" && x.IdempotencyKey == idempotencyKey,
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

            throw;
        }

        RefreshTokenResponse response = new(
            operation.Id,
            "rotated",
            "Refresh token rotated successfully.",
            newRefreshToken,
            newExpiresUtc);

        return Result<RefreshTokenResponse>.Success(response);
    }
}
