using System;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Discount.API.Extensions
{
    public static class HostExtensions
    {
        public static IHost MigrateDatabase<TContext>(this IHost host, int? retry = 0)
        {
            var retryForAvailability = 0;
            if (retry != null)
            {
                retryForAvailability = retry.Value;
            }

            using var scope = host.Services.CreateScope();
            var services = scope.ServiceProvider;
            var configuration = services.GetRequiredService<IConfiguration>();
            var logger = services.GetRequiredService<ILogger<TContext>>();

            try
            {
                logger.LogInformation("Migrating postgreSQL database.");
                using var connection = new NpgsqlConnection(configuration.GetValue<string>("DatabaseSettings:ConnectionString"));
                connection.Open();

                using var command = new NpgsqlCommand
                {
                    Connection = connection,
                    CommandText = "DROP TABLE IF EXISTS Coupon"
                };
                command.ExecuteNonQuery();

                command.CommandText = @"CREATE TABLE Coupon(
		                                    ID              SERIAL PRIMARY KEY  NOT NULL,
		                                    ProductName     VARCHAR(24) NOT NULL,
		                                    Description     TEXT,
		                                    Amount          INT
	                                    );";
                command.ExecuteNonQuery();

                command.CommandText = @"INSERT INTO Coupon (ProductName, Description, Amount) VALUES ('IPhone X', 'IPhone Discount', 150);";
                command.ExecuteNonQuery();

                command.CommandText = @"INSERT INTO Coupon (ProductName, Description, Amount) VALUES ('Samsung 10', 'Samsung Discount', 100);";
                command.ExecuteNonQuery();

            }
            catch (Exception e)
            {
                logger.LogError($"{e} - An error occurred while migrating the postgreSQL database");
                if (retryForAvailability >= 50) 
                    return host;
                retryForAvailability++;
                Thread.Sleep(2000);
                MigrateDatabase<TContext>(host, retryForAvailability);
            }

            return host;
        }
    }
}
