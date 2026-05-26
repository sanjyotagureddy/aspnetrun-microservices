using Discount.Grpc.Repositories;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Discount.Grpc.Test;

public class DiscountConnectionFactoryTests
{
  [Fact]
  public void CreateConnection_ReturnsNpgsqlConnectionWhenConfigured()
  {
    var configuration = BuildConfiguration("Host=localhost;Database=discount;Username=postgres;Password=postgres");
    var factory = new DiscountConnectionFactory(configuration);

    var connection = factory.CreateConnection();

    Assert.Contains("NpgsqlConnection", connection.GetType().Name);
  }

  [Fact]
  public void CreateConnection_ThrowsWhenConnectionStringIsMissing()
  {
    var configuration = BuildConfiguration(null);
    var factory = new DiscountConnectionFactory(configuration);

    var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateConnection());

    Assert.Contains("DatabaseSettings:ConnectionString", exception!.Message);
  }

  [Fact]
  public void Constructor_ThrowsWhenConfigurationIsNull()
  {
    Assert.Throws<ArgumentNullException>(() => new DiscountConnectionFactory(null!));
  }

  private static IConfiguration BuildConfiguration(string? connectionString)
  {
    return new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["DatabaseSettings:ConnectionString"] = connectionString
      })
      .Build();
  }
}