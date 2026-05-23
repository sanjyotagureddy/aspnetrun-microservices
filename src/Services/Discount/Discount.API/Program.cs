using System.Diagnostics.CodeAnalysis;

using Discount.API.Extensions;

namespace Discount.API;

[ExcludeFromCodeCoverage]
public class Program
{
  public static void Main(string[] args)
  {
    var host = CreateHostBuilder(args).Build();
    host.MigrateDatabase<Program>();
    host.Run();

    CreateHostBuilder(args).Build().Run();
  }

  public static IHostBuilder CreateHostBuilder(string[] args)
  {
    return Host.CreateDefaultBuilder(args)
      .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
  }
}