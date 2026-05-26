using System.Diagnostics.CodeAnalysis;

using Ordering.API.Middlewares.Extensions;
using Ordering.Infrastructure;
using Ordering.Infrastructure.Persistence;

namespace Ordering.API;

[ExcludeFromCodeCoverage]
public class Program
{
  public static void Main(string[] args)
  {
    var host = CreateHostBuilder(args).Build();
    host.MigrateDatabase<OrderContext>((context, service) =>
    {
      var logger = service.GetService<ILogger<OrderContextSeed>>();
      OrderContextSeed
        .SeedAsync(context, logger)
        .Wait();
    });
    host.Run();
  }

  public static IHostBuilder CreateHostBuilder(string[] args)
  {
    return Host.CreateDefaultBuilder(args)
      .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
  }
}