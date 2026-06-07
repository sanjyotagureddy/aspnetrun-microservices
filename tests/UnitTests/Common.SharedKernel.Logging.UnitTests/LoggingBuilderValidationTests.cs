using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Common.SharedKernel.Logging.UnitTests;

public sealed class LoggingBuilderValidationTests
{
    [Fact]
    public void AddCommonSharedKernelLogging_ShouldThrow_WhenFilePathIsInvalid()
    {
        ServiceCollection services = new();

        Action action = () => services.AddCommonSharedKernelLogging(builder =>
        {
            builder.SetServiceName("Catalog.Api");
            builder.UseFile(options => options.FilePath = " ");
        });

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*FilePath*");
    }

    [Fact]
    public void AddCommonSharedKernelLogging_ShouldThrow_WhenElasticsearchIndexNameIsInvalid()
    {
        ServiceCollection services = new();

        Action action = () => services.AddCommonSharedKernelLogging(builder =>
        {
            builder.SetServiceName("Catalog.Api");
            builder.UseElasticsearch(options => options.IndexName = " ");
        });

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*index name*");
    }

    [Fact]
    public void AddCommonSharedKernelLogging_ShouldBindPolicySensitiveKeysFromConfiguration()
    {
        Dictionary<string, string?> values = new()
        {
            ["Logging:Policy:SensitiveKeys:0"] = "customerEmail",
            ["Logging:Policy:SensitiveKeys:1"] = "phoneNumber"
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();

        ServiceCollection services = new();
        services.AddSingleton(configuration);

        services.AddCommonSharedKernelLogging(builder =>
        {
            builder.SetServiceName("Catalog.Api");
            builder.AddSink(new NoopSink());
        });

        using ServiceProvider provider = services.BuildServiceProvider();
        LoggingPolicyOptions options = provider.GetRequiredService<IOptions<LoggingPolicyOptions>>().Value;

        options.SensitiveKeys.Should().Contain("customerEmail");
        options.SensitiveKeys.Should().Contain("phoneNumber");
        options.SensitiveKeys.Should().Contain("password");
    }

    [Fact]
    public void AddCommonSharedKernelLogging_ShouldBuildProvider_WhenValidationIsEnabled()
    {
        ServiceCollection services = new();

        services.AddCommonSharedKernelLogging(builder =>
        {
            builder.SetServiceName("Catalog.Api");
            builder.AddSink(new NoopSink());
        });

        Action action = () =>
        {
            using ServiceProvider _ = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });
        };

        action.Should().NotThrow();
    }

    private sealed class NoopSink : ILogSink
    {
        public ValueTask WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;
    }
}