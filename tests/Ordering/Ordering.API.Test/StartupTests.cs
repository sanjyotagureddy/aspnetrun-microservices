using MediatR;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Ordering.Application.Contracts.Persistence;

using Xunit;

namespace Ordering.API.Test;

public sealed class StartupTests
{
    [Fact]
    public void ConfigureServices_RegistersCoreOrderingServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["ConnectionStrings:OrderingConnectionString"] = "Server=(localdb)\\mssqllocaldb;Database=OrderingTest;Trusted_Connection=True;",
                ["EventBusSettings:HostAddress"] = "amqp://guest:guest@localhost:5672",
                ["EmailSettings:FromAddress"] = "noreply@example.com",
                ["EmailSettings:ApiKey"] = "test-key",
                ["EmailSettings:FromName"] = "ordering"
            })
            .Build();

        var startup = new Startup(configuration);
        var services = new ServiceCollection();
        services.AddLogging();

        startup.ConfigureServices(services);
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IMediator>());
        Assert.NotNull(provider.GetService<IOrderRepository>());
    }
}