using Discount.Grpc.Application.Features.Discounts.Commands.CreateDiscount;
using Discount.Grpc.Application.Features.Discounts.Commands.DeleteDiscount;
using Discount.Grpc.Application.Features.Discounts.Commands.UpdateDiscount;
using Discount.Grpc.Application.Features.Discounts.Queries.GetDiscount;
using Discount.Grpc.Entities;
using Discount.Grpc.Repositories.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using System.Text;

namespace Discount.Grpc.Test;

public class StartupTests
{
  [Fact]
  public void ConfigureServices_RegistersMediatorAndInfrastructureServices()
  {
    var startup = new Startup();
    var services = new ServiceCollection();
    services.AddSingleton<IConfiguration>(BuildConfiguration());

    startup.ConfigureServices(services);

    using var provider = services.BuildServiceProvider();

    Assert.NotNull(provider.GetRequiredService<IMediator>());
    Assert.Contains("CouponRepository", provider.GetRequiredService<ICouponRepository>().GetType().Name);
    Assert.NotNull(provider.GetRequiredService<IDiscountConnectionFactory>());
    Assert.NotNull(provider.GetRequiredService<IDiscountDatabaseInitializer>());
  }

  [Fact]
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

    Assert.Equal(coupon, queryResult);
    Assert.Equal(coupon, createResult);
    Assert.Equal(coupon, updateResult);
    Assert.True(deleteResult);
  }

  [Fact]
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

    var exception = Record.Exception(() => startup.Configure(app, environment.Object));
    Assert.Null(exception);
  }

  [Fact]
  public async Task WriteDefaultResponseAsync_WritesGrpcClientGuidance()
  {
    var context = new DefaultHttpContext();
    using var responseStream = new MemoryStream();
    context.Response.Body = responseStream;

    await Startup.WriteDefaultResponseAsync(context);

    responseStream.Position = 0;
    using var reader = new StreamReader(responseStream, Encoding.UTF8);
    var body = await reader.ReadToEndAsync();

    Assert.Contains("gRPC client", body);
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