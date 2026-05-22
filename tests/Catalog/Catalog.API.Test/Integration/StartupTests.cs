using Catalog.API;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Catalog.API.Test;

public class StartupTests
{
    [Fact]
    public void ConfigureServices_UsesMemoryCacheWhenRedisIsDisabled()
    {
        var startup = new Startup(BuildConfiguration(useRedis: false));
        var services = new ServiceCollection();

        startup.ConfigureServices(services);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IDistributedCache>().GetType().Name.Should().Contain("MemoryDistributedCache");
    }

    [Fact]
    public void ConfigureServices_UsesRedisCacheWhenRedisIsEnabled()
    {
        var startup = new Startup(BuildConfiguration(useRedis: true));
        var services = new ServiceCollection();

        startup.ConfigureServices(services);

        using var provider = services.BuildServiceProvider();
        provider.GetRequiredService<IDistributedCache>().GetType().Name.Should().Contain("RedisCache");
    }

    private static IConfiguration BuildConfiguration(bool useRedis)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CacheSettings:UseRedis"] = useRedis.ToString(),
                ["CacheSettings:ConnectionString"] = "localhost:6379",
                ["DatabaseSettings:ConnectionString"] = "mongodb://localhost:27017",
                ["DatabaseSettings:DatabaseName"] = "ProductDb",
                ["DatabaseSettings:CollectionName"] = "Products"
            })
            .Build();
    }
}