using Dapper;
using Microsoft.Data.Sqlite;

namespace Discount.Grpc.Data;

public static class DiscountDbInitializer
{
    public static async Task InitializeDbAsync(IConfiguration configuration, ILogger logger)
    {
        var connectionString = configuration.GetValue<string>("DatabaseSettings:ConnectionString")
                               ?? "Data Source=discountdb.db";

        try
        {
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync();

            const string createTableSql = """
                CREATE TABLE IF NOT EXISTS Coupons (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProductName TEXT NOT NULL,
                    Description TEXT,
                    Amount INTEGER NOT NULL
                );
                """;

            await connection.ExecuteAsync(createTableSql);

            var count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(1) FROM Coupons");
            if (count == 0)
            {
                const string seedSql = """
                    INSERT INTO Coupons (ProductName, Description, Amount) VALUES
                    ('IPhone X', 'IPhone Discount', 150),
                    ('Samsung S10', 'Samsung Discount', 100);
                    """;
                await connection.ExecuteAsync(seedSql);
                logger.LogInformation("Database seeded with default discount coupons.");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the Discount SQLite database.");
            throw;
        }
    }
}
