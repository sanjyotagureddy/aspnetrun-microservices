using Basket.API.Application.Contracts.Infrastructure;
using Basket.API.Application.Contracts.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MediatR;
using Xunit;

namespace Basket.API.Test;

public sealed class StartupTests
{
    [Fact]
    public void ConfigureServices_RegistersBasketServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["CacheSettings:ConnectionString"] = "localhost:6379",
                ["GrpcSettings:DiscountUrl"] = "http://localhost:5001",
                ["EventBusSettings:HostAddress"] = "amqp://localhost"
            })
            .Build();

        var startup = new Startup(configuration);
        var services = new ServiceCollection();

        startup.ConfigureServices(services);

        ServiceProvider provider = services.BuildServiceProvider();

        Assert.NotNull(provider.GetService<IMediator>());
        Assert.NotNull(provider.GetService<IBasketRepository>());
        Assert.NotNull(provider.GetService<IDiscountService>());
        Assert.NotNull(provider.GetService<IBasketCheckoutPublisher>());
    }

}