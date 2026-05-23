using Discount.Grpc.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;

namespace Discount.Grpc.Test;

[TestFixture]
public class ProgramTests
{
  [Test]
  public void CreateHostBuilder_ReturnsHostBuilder()
  {
    var hostBuilder = Program.CreateHostBuilder(Array.Empty<string>());

    Assert.That(hostBuilder, Is.Not.Null);
  }

  [Test]
  public void Main_UsesInjectedHostBuilderAndRunner()
  {
    var initializer = new Mock<IDiscountDatabaseInitializer>();
    Program.HostBuilderFactory = _ => Host.CreateDefaultBuilder()
      .ConfigureServices(services =>
      {
        services.AddLogging();
        services.AddSingleton(initializer.Object);
      });
    Program.HostRunner = _ => { };

    try
    {
      Program.Main(Array.Empty<string>());
    }
    finally
    {
      Program.HostBuilderFactory = Program.CreateHostBuilder;
      Program.HostRunner = host => host.Run();
    }

    initializer.Verify(initializer => initializer.Initialize(), Times.Once);
  }
}
