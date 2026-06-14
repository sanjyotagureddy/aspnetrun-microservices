using System.Security.Cryptography;
using System.Text;
using Auth.Api.Contracts;
using Auth.Api.Features.Auth.Login;
using Auth.Api.Infrastructure.Persistence;
using Common.SharedKernel.Results;
using FluentAssertions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api.Tests.Features.Auth.Login;

public sealed class ExchangeCallbackCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Fail_When_CodeVerifier_Does_Not_Match_Challenge()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AuthDbContext dbContext = CreateDbContext();
        DateTime utcNow = new(2026, 6, 14, 10, 0, 0, DateTimeKind.Utc);
        TestTimeProvider timeProvider = new(utcNow);

        LoginTransaction transaction = new()
        {
            Id = Guid.NewGuid(),
            ClientId = "web-app",
            State = "state-1",
            Nonce = "nonce-1",
            RedirectUri = "http://localhost:5173/auth/callback",
            CodeChallengeMethod = "S256",
            CodeChallenge = ComputeS256("expected-verifier"),
            CreatedUtc = utcNow.AddMinutes(-2),
            ExpiresUtc = utcNow.AddMinutes(8)
        };

        dbContext.LoginTransactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        ExchangeCallbackCommandHandler handler = new(dbContext, timeProvider);

        Result<ExchangeCallbackResponse> result = await handler.Handle(
            new ExchangeCallbackCommand(new ExchangeCallbackRequest(
                transaction.State,
                "auth-code",
                "wrong-verifier",
                transaction.RedirectUri,
                transaction.ClientId)),
            cancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("PKCE code verifier is invalid");
    }

    [Fact]
    public async Task Handle_Should_Fail_When_ClientId_Does_Not_Match_Initial_Request()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AuthDbContext dbContext = CreateDbContext();
        DateTime utcNow = new(2026, 6, 14, 11, 0, 0, DateTimeKind.Utc);
        TestTimeProvider timeProvider = new(utcNow);

        string verifier = "verifier-for-client-mismatch-test-value-123456789";

        LoginTransaction transaction = new()
        {
            Id = Guid.NewGuid(),
            ClientId = "web-app",
            State = "state-2",
            Nonce = "nonce-2",
            RedirectUri = "http://localhost:5173/auth/callback",
            CodeChallengeMethod = "S256",
            CodeChallenge = ComputeS256(verifier),
            CreatedUtc = utcNow.AddMinutes(-2),
            ExpiresUtc = utcNow.AddMinutes(8)
        };

        dbContext.LoginTransactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        ExchangeCallbackCommandHandler handler = new(dbContext, timeProvider);

        Result<ExchangeCallbackResponse> result = await handler.Handle(
            new ExchangeCallbackCommand(new ExchangeCallbackRequest(
                transaction.State,
                "auth-code",
                verifier,
                transaction.RedirectUri,
                "another-client")),
            cancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Client ID does not match");
    }

    [Fact]
    public async Task Handle_Should_Issue_RefreshToken_When_Pkce_And_ClientBinding_Are_Valid()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AuthDbContext dbContext = CreateDbContext();
        DateTime utcNow = new(2026, 6, 14, 12, 0, 0, DateTimeKind.Utc);
        TestTimeProvider timeProvider = new(utcNow);

        string verifier = "valid-verifier-with-required-length-1234567890";

        LoginTransaction transaction = new()
        {
            Id = Guid.NewGuid(),
            ClientId = "web-app",
            State = "state-3",
            Nonce = "nonce-3",
            RedirectUri = "http://localhost:5173/auth/callback",
            CodeChallengeMethod = "S256",
            CodeChallenge = ComputeS256(verifier),
            CreatedUtc = utcNow.AddMinutes(-2),
            ExpiresUtc = utcNow.AddMinutes(8)
        };

        dbContext.LoginTransactions.Add(transaction);
        await dbContext.SaveChangesAsync(cancellationToken);

        ExchangeCallbackCommandHandler handler = new(dbContext, timeProvider);

        Result<ExchangeCallbackResponse> result = await handler.Handle(
            new ExchangeCallbackCommand(new ExchangeCallbackRequest(
                transaction.State,
                "auth-code",
                verifier,
                transaction.RedirectUri,
                transaction.ClientId)),
            cancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Status.Should().Be("issued");
        result.Value.RefreshToken.Should().NotBeNullOrWhiteSpace();

        LoginTransaction persisted = await dbContext.LoginTransactions.SingleAsync(x => x.Id == transaction.Id, cancellationToken);
        persisted.ExchangeCompletedUtc.Should().Be(utcNow);

        int refreshCount = await dbContext.RefreshTokenGrants.CountAsync(cancellationToken);
        refreshCount.Should().Be(1);
    }

    private static string ComputeS256(string verifier)
    {
        byte[] hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return WebEncoders.Base64UrlEncode(hash);
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
