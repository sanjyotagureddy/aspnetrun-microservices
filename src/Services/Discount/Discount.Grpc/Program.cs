using Discount.Grpc.Extensions;

namespace Discount.Grpc;

public class Program
{
  internal static Func<string[], IHostBuilder> HostBuilderFactory { get; set; } = CreateHostBuilder;

  internal static Action<IHost> HostRunner { get; set; } = host => host.Run();

  public static void Main(string[] args)
  {
    var host = HostBuilderFactory(args).Build();
    host.MigrateDatabase<Program>();
    HostRunner(host);
  }

  // Additional configuration is required to successfully run gRPC on macOS.
  // For instructions on how to configure Kestrel and gRPC clients on macOS, visit https://go.microsoft.com/fwlink/?linkid=2099682
  public static IHostBuilder CreateHostBuilder(string[] args)
  {
    return Host.CreateDefaultBuilder(args)
      .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); });
  }
}