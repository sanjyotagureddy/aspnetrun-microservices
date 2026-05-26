using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Ordering.Infrastructure;

[ExcludeFromCodeCoverage]
public class OrderContextFactory : IDesignTimeDbContextFactory<OrderContext>
{
    public OrderContext CreateDbContext(string[] args)
    {
        // Build configuration to fetch the connection string
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json") // Point to the file with your ConnectionStrings
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<OrderContext>();
        var connectionString = configuration.GetConnectionString("OrderingConnectionString");

        // Use your specific provider here (e.g., UseSqlServer)
        optionsBuilder.UseSqlServer(connectionString);

        return new OrderContext(optionsBuilder.Options);
    }
}
