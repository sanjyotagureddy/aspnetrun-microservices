using Discount.Grpc.Repositories.Interfaces;

namespace Discount.Grpc.Extensions;

public static class HostExtensions
{
  public static IHost MigrateDatabase<TContext>(this IHost host, int? retry = 0, TimeSpan? retryDelay = null)
  {
    var retryForAvailability = 0;
    if (retry != null) retryForAvailability = retry.Value;
    var delay = retryDelay ?? TimeSpan.FromSeconds(2);

    using var scope = host.Services.CreateScope();
    var services = scope.ServiceProvider;
    var initializer = services.GetRequiredService<IDiscountDatabaseInitializer>();
    var logger = services.GetRequiredService<ILogger<TContext>>();

    try
    {
      logger.LogInformation("Migrating postgreSQL database.");
      initializer.Initialize();
    }
    catch (Exception e)
    {
      logger.LogError($"{e} - An error occurred while migrating the postgreSQL database");
      if (retryForAvailability >= 50)
        return host;
      retryForAvailability++;
      if (delay > TimeSpan.Zero)
      {
        Thread.Sleep(delay);
      }

      MigrateDatabase<TContext>(host, retryForAvailability, delay);
    }

    return host;
  }
}