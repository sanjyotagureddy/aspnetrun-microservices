using System.Security.Claims;
using Auth.Api.Contracts;
using Auth.Api.Features.Auth.Tokens;
using Auth.Api.Infrastructure.Persistence;
using Auth.Api.Infrastructure.Security;
using Common.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Tests.Features.Auth.Tokens;

public sealed class LogoutCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Revoke_RefreshToken_Family_When_RefreshToken_Provided()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AuthDbContext dbContext = CreateDbContext();
        DateTime utcNow = new(2026, 6, 14, 10, 0, 0, DateTimeKind.Utc);
        TestTimeProvider timeProvider = new(utcNow);

        Guid familyId = Guid.NewGuid();
        string refreshToken = "logout-token";

        dbContext.RefreshTokenGrants.AddRange(
            new RefreshTokenGrant
            {
                Id = Guid.NewGuid(),
                FamilyId = familyId,
                TokenHash = RefreshTokenCrypto.Hash(refreshToken),
                Subject = "user-1",
                IssuedUtc = utcNow.AddHours(-1),
                ExpiresUtc = utcNow.AddDays(10)
            },
            new RefreshTokenGrant
            {
                Id = Guid.NewGuid(),
                FamilyId = familyId,
                TokenHash = RefreshTokenCrypto.Hash("sibling-token"),
                Subject = "user-1",
                IssuedUtc = utcNow.AddMinutes(-30),
                ExpiresUtc = utcNow.AddDays(10)
            });

        await dbContext.SaveChangesAsync(cancellationToken);

        LogoutCommandHandler handler = new(dbContext, timeProvider);

        ClaimsPrincipal user = CreateUser("user-1");
        Result<LogoutResponse> result = await handler.Handle(
            new LogoutCommand(new LogoutRequest("session-1", refreshToken, "jti-1"), user),
            cancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be("recorded");

        List<RefreshTokenGrant> family = await dbContext.RefreshTokenGrants
            .AsNoTracking()
            .Where(x => x.FamilyId == familyId)
            .ToListAsync(cancellationToken);

        family.Should().OnlyContain(x => x.RevokedUtc == utcNow && x.RevocationReason == "logout");

        TokenOperation operation = await dbContext.TokenOperations.SingleAsync(cancellationToken);
        operation.OperationType.Should().Be("logout");
        operation.Subject.Should().Be("user-1");
        operation.SessionId.Should().Be("session-1");
        operation.AccessTokenJti.Should().Be("jti-1");
        operation.RefreshTokenHash.Should().Be(RefreshTokenCrypto.Hash(refreshToken));
    }

    [Fact]
    public async Task Handle_Should_Record_Operation_When_RefreshToken_Is_Missing()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AuthDbContext dbContext = CreateDbContext();
        DateTime utcNow = new(2026, 6, 14, 11, 0, 0, DateTimeKind.Utc);
        TestTimeProvider timeProvider = new(utcNow);

        LogoutCommandHandler handler = new(dbContext, timeProvider);

        ClaimsPrincipal user = CreateUser("user-2");
        Result<LogoutResponse> result = await handler.Handle(
            new LogoutCommand(new LogoutRequest("session-2", null, null), user),
            cancellationToken);

        result.IsSuccess.Should().BeTrue();

        TokenOperation operation = await dbContext.TokenOperations.SingleAsync(cancellationToken);
        operation.OperationType.Should().Be("logout");
        operation.Subject.Should().Be("user-2");
        operation.SessionId.Should().Be("session-2");
        operation.RefreshTokenHash.Should().BeNull();
    }

    private static ClaimsPrincipal CreateUser(string subject)
    {
        ClaimsIdentity identity = new(
        [
            new Claim("sub", subject)
        ],
            authenticationType: "test");

        return new ClaimsPrincipal(identity);
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
