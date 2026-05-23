using Discount.Grpc.Extensions;
using Discount.Grpc.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;

namespace Discount.Grpc.Test;

[TestFixture]
public class HostExtensionsTests
{
  [Test]
  public void MigrateDatabase_RetriesOnceWhenInitializerFails()
  {
    var initializer = new Mock<IDiscountDatabaseInitializer>();
    var attempts = 0;

    initializer.Setup(initializer => initializer.Initialize())
      .Callback(() =>
      {
        attempts++;
        if (attempts == 1)
        {
          throw new InvalidOperationException("Transient failure");
        }
      });

    using var host = Host.CreateDefaultBuilder()
      .ConfigureServices(services =>
      {
        services.AddLogging();
        services.AddSingleton(initializer.Object);
      })
      .Build();

    var result = host.MigrateDatabase<HostExtensionsTests>(retryDelay: TimeSpan.Zero);

    Assert.That(result, Is.SameAs(host));
    Assert.That(attempts, Is.EqualTo(2));
  }

  [Test]
  public void MigrateDatabase_UsesDefaultsWhenRetryArgumentsAreMissing()
  {
    var initializer = new Mock<IDiscountDatabaseInitializer>();
    initializer.Setup(initializer => initializer.Initialize());

    using var host = Host.CreateDefaultBuilder()
      .ConfigureServices(services =>
      {
        services.AddLogging();
        services.AddSingleton(initializer.Object);
      })
      .Build();

    var result = host.MigrateDatabase<HostExtensionsTests>(retry: null, retryDelay: null);

    Assert.That(result, Is.SameAs(host));
    initializer.Verify(initializer => initializer.Initialize(), Times.Once);
  }

  [Test]
  public void MigrateDatabase_SleepsAndRetriesWithCustomDelay()
  {
    var initializer = new Mock<IDiscountDatabaseInitializer>();
    var attempts = 0;

    initializer.Setup(initializer => initializer.Initialize())
      .Callback(() =>
      {
        attempts++;
        if (attempts == 1)
        {
          throw new InvalidOperationException("Transient failure");
        }
      });

    using var host = Host.CreateDefaultBuilder()
      .ConfigureServices(services =>
      {
        services.AddLogging();
        services.AddSingleton(initializer.Object);
      })
      .Build();

    var result = host.MigrateDatabase<HostExtensionsTests>(retryDelay: TimeSpan.FromMilliseconds(1));

    Assert.That(result, Is.SameAs(host));
    Assert.That(attempts, Is.EqualTo(2));
  }

  [Test]
  public void MigrateDatabase_ReturnsImmediatelyAfterRetryLimit()
  {
    var initializer = new Mock<IDiscountDatabaseInitializer>();
    initializer.Setup(initializer => initializer.Initialize()).Throws<InvalidOperationException>();

    using var host = Host.CreateDefaultBuilder()
      .ConfigureServices(services =>
      {
        services.AddLogging();
        services.AddSingleton(initializer.Object);
      })
      .Build();

    var result = host.MigrateDatabase<HostExtensionsTests>(retry: 50, retryDelay: TimeSpan.Zero);

    Assert.That(result, Is.SameAs(host));
    initializer.Verify(initializer => initializer.Initialize(), Times.Once);
  }
}