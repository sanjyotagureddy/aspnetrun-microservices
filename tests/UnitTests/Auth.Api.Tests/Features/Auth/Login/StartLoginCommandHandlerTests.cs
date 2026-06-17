using Auth.Api.Contracts;
using Auth.Api.Features.Auth.Login;
using Auth.Api.Infrastructure.Configuration;
using Auth.Api.Infrastructure.Persistence;
using Common.SharedKernel.Results;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auth.Api.Tests.Features.Auth.Login;

public sealed class StartLoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_Should_Persist_ClientId_On_LoginTransaction()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AuthDbContext dbContext = CreateDbContext();
        DateTime utcNow = new(2026, 6, 14, 13, 0, 0, DateTimeKind.Utc);
        TestTimeProvider timeProvider = new(utcNow);

        AuthOptions options = new()
        {
            Authority = "http://localhost:8080/realms/commerce",
            Issuer = "http://localhost:8080/realms/commerce",
            DiscoveryUrl = "http://localhost:8080/realms/commerce/.well-known/openid-configuration",
            JwksUrl = "http://localhost:8080/realms/commerce/protocol/openid-connect/certs",
            Audience = "auth-api",
            WebClientId = "web-app",
            WebClientScope = "openid profile email",
            PkceClients =
            [
                new PkceClientOptions
                {
                    ClientId = "web-app",
                    Scope = "openid profile email",
                    RedirectUris = ["http://localhost:5173/auth/callback"]
                },
                new PkceClientOptions
                {
                    ClientId = "mobile-app",
                    Scope = "openid profile",
                    RedirectUris = ["http://localhost:3000/auth/callback"]
                }
            ]
        };

        StartLoginCommandHandler handler = new(dbContext, Options.Create(options), timeProvider);

        Result<StartLoginResponse> result = await handler.Handle(
            new StartLoginCommand(new StartLoginRequest(
                "mobile-app",
                "http://localhost:3000/auth/callback",
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                "S256",
                "state-test",
                "nonce-test",
                null)),
            cancellationToken);

        result.IsSuccess.Should().BeTrue();

        LoginTransaction transaction = await dbContext.LoginTransactions.SingleAsync(x => x.State == "state-test", cancellationToken);
        transaction.ClientId.Should().Be("mobile-app");
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
