using Discount.Grpc.Application.Features.Discounts.Commands.CreateDiscount;
using Discount.Grpc.Application.Features.Discounts.Commands.DeleteDiscount;
using Discount.Grpc.Application.Features.Discounts.Commands.UpdateDiscount;
using Discount.Grpc.Application.Features.Discounts.Queries.GetDiscount;
using Discount.Grpc;
using Discount.Grpc.Entities;
using Discount.Grpc.Repositories.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using System.Text;

namespace Discount.Grpc.Test;

[TestFixture]
public class StartupTests
{
  [Test]
  public void ConfigureServices_RegistersMediatorAndInfrastructureServices()
  {
    var startup = new Startup();
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(BuildConfiguration());

    startup.ConfigureServices(services);

    using var provider = services.BuildServiceProvider();

    Assert.That(provider.GetRequiredService<IMediator>(), Is.Not.Null);
    Assert.That(provider.GetRequiredService<ICouponRepository>().GetType().Name, Does.Contain("CouponRepository"));
    Assert.That(provider.GetRequiredService<IDiscountConnectionFactory>(), Is.Not.Null);
    Assert.That(provider.GetRequiredService<IDiscountDatabaseInitializer>(), Is.Not.Null);
  }

  [Test]
  public async Task Mediator_ExecutesRegisteredCQRSHandlers()
  {
    var startup = new Startup();
    var services = new ServiceCollection();
    var repository = new Mock<ICouponRepository>();
    var coupon = new Coupon { Id = 1, ProductName = "IPhone X", Description = "IPhone Discount", Amount = 150 };

    services.AddSingleton<IConfiguration>(BuildConfiguration());
    startup.ConfigureServices(services);
    services.AddSingleton(repository.Object);

    repository.Setup(repository => repository.GetByProductNameAsync("IPhone X", It.IsAny<CancellationToken>()))
      .ReturnsAsync(coupon);
    repository.Setup(repository => repository.CreateAsync(It.IsAny<Coupon>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(true);
    repository.Setup(repository => repository.UpdateAsync(It.IsAny<Coupon>(), It.IsAny<CancellationToken>()))
      .ReturnsAsync(true);
    repository.Setup(repository => repository.DeleteAsync("IPhone X", It.IsAny<CancellationToken>()))
      .ReturnsAsync(true);

    using var provider = services.BuildServiceProvider();
    var mediator = provider.GetRequiredService<IMediator>();

    var queryResult = await mediator.Send(new GetDiscountQuery("IPhone X"));
    var createResult = await mediator.Send(new CreateDiscountCommand(coupon));
    var updateResult = await mediator.Send(new UpdateDiscountCommand(coupon));
    var deleteResult = await mediator.Send(new DeleteDiscountCommand("IPhone X"));

    Assert.That(queryResult, Is.EqualTo(coupon));
    Assert.That(createResult, Is.EqualTo(coupon));
    Assert.That(updateResult, Is.EqualTo(coupon));
    Assert.That(deleteResult, Is.True);
  }

  [Test]
  public void Configure_BuildsPipelineWithoutThrowing()
  {
    var startup = new Startup();
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(BuildConfiguration());
    services.AddLogging();
    startup.ConfigureServices(services);

    using var provider = services.BuildServiceProvider();
    var app = new Microsoft.AspNetCore.Builder.ApplicationBuilder(provider);
    var environment = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
    environment.SetupGet(environment => environment.EnvironmentName).Returns(Microsoft.Extensions.Hosting.Environments.Development);

    Assert.DoesNotThrow(() => startup.Configure(app, environment.Object));
  }

  [Test]
  public async Task WriteDefaultResponseAsync_WritesGrpcClientGuidance()
  {
    var context = new DefaultHttpContext();
    using var responseStream = new MemoryStream();
    context.Response.Body = responseStream;

    await Startup.WriteDefaultResponseAsync(context);

    responseStream.Position = 0;
    using var reader = new StreamReader(responseStream, Encoding.UTF8);
    var body = await reader.ReadToEndAsync();

    Assert.That(body, Does.Contain("gRPC client"));
  }

  private static IConfiguration BuildConfiguration()
  {
    return new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["DatabaseSettings:ConnectionString"] = "Host=localhost;Database=discount;Username=postgres;Password=postgres"
      })
      .Build();
  }
}