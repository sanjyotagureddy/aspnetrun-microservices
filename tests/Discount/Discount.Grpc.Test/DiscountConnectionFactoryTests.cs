using Discount.Grpc.Repositories;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Discount.Grpc.Test;

[TestFixture]
public class DiscountConnectionFactoryTests
{
  [Test]
  public void CreateConnection_ReturnsNpgsqlConnectionWhenConfigured()
  {
    var configuration = BuildConfiguration("Host=localhost;Database=discount;Username=postgres;Password=postgres");
    var factory = new DiscountConnectionFactory(configuration);

    var connection = factory.CreateConnection();

    Assert.That(connection.GetType().Name, Does.Contain("NpgsqlConnection"));
  }

  [Test]
  public void CreateConnection_ThrowsWhenConnectionStringIsMissing()
  {
    var configuration = BuildConfiguration(null);
    var factory = new DiscountConnectionFactory(configuration);

    var exception = Assert.Throws<InvalidOperationException>(() => factory.CreateConnection());

    Assert.That(exception!.Message, Does.Contain("DatabaseSettings:ConnectionString"));
  }

  [Test]
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