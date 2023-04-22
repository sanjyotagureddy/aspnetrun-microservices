using System;
using System.Threading;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Ordering.API.Extensions;

public static class HostExtension
{
  public static IHost MigrateDatabase<TContext>(this IHost host, Action<TContext,
      IServiceProvider> seeder,
    int? retry = 0) where TContext : DbContext
  {
    var retryForAvailability = 0;
    if (retry != null) retryForAvailability = retry.Value;

    using var scope = host.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<TContext>>();
    var context = services.GetService<TContext>();

    try
    {
      logger.LogInformation("Migrating database associated with context {DbContextName}", typeof(TContext).Name);

      InvokeSeeder(seeder, context, services);

      logger.LogInformation("Migrated database associated with context {DbContextName}", typeof(TContext).Name);
    }
    catch (SqlException ex)
    {
      logger.LogError(ex, "An error occurred while migrating the database used on context {DbContextName}",
        typeof(TContext).Name);

      if (retryForAvailability >= 50)
        return host;

      retryForAvailability++;
      Thread.Sleep(2000);
      MigrateDatabase(host, seeder, retryForAvailability);
    }

    return host;
  }

  private static void InvokeSeeder<TContext>(Action<TContext, IServiceProvider> seeder, TContext context,
    IServiceProvider services) where TContext : DbContext
  {
    context.Database.Migrate();
    seeder(context, services);
  }
}