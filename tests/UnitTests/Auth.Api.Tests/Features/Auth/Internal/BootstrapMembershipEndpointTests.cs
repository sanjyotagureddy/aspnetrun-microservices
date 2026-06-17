using System.Reflection;
using Auth.Api.Contracts;
using Auth.Api.Features.Auth.Internal;
using Auth.Api.Infrastructure.Configuration;
using Auth.Api.Infrastructure.Persistence;
using Auth.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.FileProviders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Auth.Api.Tests.Features.Auth.Internal;

public sealed class BootstrapMembershipEndpointTests
{
    [Fact]
    public async Task HandleAsync_Should_Return_NotFound_When_Not_Development()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AuthDbContext dbContext = CreateDbContext();
        IHostEnvironment environment = new TestHostEnvironment("Production");

        IResult result = await InvokeHandleAsync(
            new BootstrapMembershipRequest("sub-1", "tenant-1", TenantRoleNames.PlatformAdmin, "secret"),
            Options.Create(new DevBootstrapOptions { Enabled = true, SharedSecret = "secret" }),
            environment,
            dbContext,
            new TestTimeProvider(new DateTime(2026, 6, 14, 12, 0, 0, DateTimeKind.Utc)),
            cancellationToken);

        result.Should().BeOfType<NotFound>();
    }

    [Fact]
    public async Task HandleAsync_Should_Return_Forbidden_When_SharedSecret_Does_Not_Match()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AuthDbContext dbContext = CreateDbContext();
        IHostEnvironment environment = new TestHostEnvironment(Environments.Development);

        IResult result = await InvokeHandleAsync(
            new BootstrapMembershipRequest("sub-1", "tenant-1", TenantRoleNames.PlatformAdmin, "wrong-secret"),
            Options.Create(new DevBootstrapOptions { Enabled = true, SharedSecret = "expected-secret" }),
            environment,
            dbContext,
            new TestTimeProvider(new DateTime(2026, 6, 14, 12, 30, 0, DateTimeKind.Utc)),
            cancellationToken);

        result.Should().BeOfType<ForbidHttpResult>();
    }

    [Fact]
    public async Task HandleAsync_Should_Create_Membership_When_Request_Is_Valid()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using AuthDbContext dbContext = CreateDbContext();
        IHostEnvironment environment = new TestHostEnvironment(Environments.Development);
        DateTime utcNow = new(2026, 6, 14, 13, 0, 0, DateTimeKind.Utc);

        BootstrapMembershipRequest request = new("sub-1", "tenant-1", TenantRoleNames.PlatformAdmin, "secret");

        IResult result = await InvokeHandleAsync(
            request,
            Options.Create(new DevBootstrapOptions { Enabled = true, SharedSecret = "secret" }),
            environment,
            dbContext,
            new TestTimeProvider(utcNow),
            cancellationToken);

        Created<BootstrapMembershipResponse> created = result.Should().BeOfType<Created<BootstrapMembershipResponse>>().Subject;
        created.Value.Should().NotBeNull();
        created.Value!.Status.Should().Be("created");

        UserTenantMembership membership = await dbContext.UserTenantMemberships.SingleAsync(cancellationToken);
        membership.Subject.Should().Be(request.Subject);
        membership.TenantId.Should().Be(request.TenantId);
        membership.Role.Should().Be(TenantRoleNames.PlatformAdmin);
        membership.CreatedUtc.Should().Be(utcNow);
    }

    private static async Task<IResult> InvokeHandleAsync(
        BootstrapMembershipRequest request,
        IOptions<DevBootstrapOptions> options,
        IHostEnvironment environment,
        AuthDbContext dbContext,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        MethodInfo method = typeof(BootstrapMembershipEndpoint)
            .GetMethod("HandleAsync", BindingFlags.NonPublic | BindingFlags.Static)!;

        Task<IResult> task = (Task<IResult>)method.Invoke(null, new object?[] { request, options, environment, dbContext, timeProvider, cancellationToken })!;
        return await task;
    }

    private static AuthDbContext CreateDbContext()
    {
        DbContextOptions<AuthDbContext> options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AuthDbContext(options);
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "Auth.Api.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class TestTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
