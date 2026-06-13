using Auth.Api.Features.Auth.Tokens;
using Auth.Api.Contracts;
using Auth.Api.Infrastructure.Persistence;
using Auth.Api.Infrastructure.Security;
using Common.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Tests.Features.Auth.Tokens;

public sealed class RefreshTokenCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Rotate_Token_When_Current_Token_Is_Active()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AuthDbContext dbContext = CreateDbContext();
        DateTime utcNow = new(2026, 6, 13, 12, 0, 0, DateTimeKind.Utc);
        TestTimeProvider timeProvider = new(utcNow);

        string currentToken = "active-token";
        RefreshTokenGrant grant = new()
        {
            Id = Guid.NewGuid(),
            FamilyId = Guid.NewGuid(),
            TokenHash = RefreshTokenCrypto.Hash(currentToken),
            Subject = "user-1",
            IssuedUtc = utcNow.AddMinutes(-10),
            ExpiresUtc = utcNow.AddDays(14)
        };

        dbContext.RefreshTokenGrants.Add(grant);
        await dbContext.SaveChangesAsync(cancellationToken);

        RefreshTokenCommandHandler handler = new(dbContext, timeProvider);

        Result<RefreshTokenResponse> result = await handler.Handle(
            new RefreshTokenCommand(new RefreshTokenRequest(currentToken, null)),
            cancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be("rotated");
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();

        List<RefreshTokenGrant> family = await dbContext.RefreshTokenGrants
            .Where(x => x.FamilyId == grant.FamilyId)
            .ToListAsync(cancellationToken);

        family.Should().HaveCount(2);
        family.Single(x => x.TokenHash == grant.TokenHash).ConsumedUtc.Should().Be(utcNow);
        family.Single(x => x.TokenHash != grant.TokenHash).ParentTokenHash.Should().Be(grant.TokenHash);
    }

    [Fact]
    public async Task Handle_Should_Revoke_Family_When_Reused_Token_Is_Submitted()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AuthDbContext dbContext = CreateDbContext();
        DateTime utcNow = new(2026, 6, 13, 13, 0, 0, DateTimeKind.Utc);
        TestTimeProvider timeProvider = new(utcNow);

        Guid familyId = Guid.NewGuid();
        string consumedToken = "consumed-token";

        dbContext.RefreshTokenGrants.AddRange(
            new RefreshTokenGrant
            {
                Id = Guid.NewGuid(),
                FamilyId = familyId,
                TokenHash = RefreshTokenCrypto.Hash(consumedToken),
                Subject = "user-1",
                IssuedUtc = utcNow.AddHours(-2),
                ExpiresUtc = utcNow.AddDays(14),
                ConsumedUtc = utcNow.AddHours(-1)
            },
            new RefreshTokenGrant
            {
                Id = Guid.NewGuid(),
                FamilyId = familyId,
                TokenHash = RefreshTokenCrypto.Hash("active-child-token"),
                Subject = "user-1",
                IssuedUtc = utcNow.AddHours(-1),
                ExpiresUtc = utcNow.AddDays(14)
            });

        await dbContext.SaveChangesAsync(cancellationToken);

        RefreshTokenCommandHandler handler = new(dbContext, timeProvider);

        Result<RefreshTokenResponse> result = await handler.Handle(
            new RefreshTokenCommand(new RefreshTokenRequest(consumedToken, null)),
            cancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("reuse detected");

        List<RefreshTokenGrant> family = await dbContext.RefreshTokenGrants
            .Where(x => x.FamilyId == familyId)
            .ToListAsync(cancellationToken);

        family.Should().OnlyContain(x => x.RevokedUtc == utcNow && x.RevocationReason == "reuse-detected");
    }

    [Fact]
    public async Task Handle_Should_Return_Failure_For_Unknown_Token()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AuthDbContext dbContext = CreateDbContext();
        TestTimeProvider timeProvider = new(new DateTime(2026, 6, 13, 14, 0, 0, DateTimeKind.Utc));

        RefreshTokenCommandHandler handler = new(dbContext, timeProvider);

        Result<RefreshTokenResponse> result = await handler.Handle(
            new RefreshTokenCommand(new RefreshTokenRequest("unknown-token", null)),
            cancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Invalid refresh token.");
    }

    private static AuthDbContext CreateDbContext()
    {
        DbContextOptions<AuthDbContext> options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AuthDbContext(options);
    }

    private sealed class TestTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
