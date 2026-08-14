using Dapper;
using Npgsql;

namespace Order.Api.Data;

public class OrderDbInitializer : Microsoft.Extensions.Hosting.IHostedService
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly ILogger<OrderDbInitializer> _logger;

    public OrderDbInitializer(NpgsqlDataSource dataSource, ILogger<OrderDbInitializer> logger)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

            const string sql = """
                CREATE TABLE IF NOT EXISTS orders (
                    id UUID PRIMARY KEY,
                    user_name VARCHAR(100) NOT NULL,
                    total_price NUMERIC(18,2) NOT NULL,
                    first_name VARCHAR(100),
                    last_name VARCHAR(100),
                    email_address VARCHAR(150),
                    address_line VARCHAR(200),
                    country VARCHAR(100),
                    state VARCHAR(100),
                    zip_code VARCHAR(20),
                    card_name VARCHAR(100),
                    card_number VARCHAR(50),
                    expiration VARCHAR(20),
                    cvv VARCHAR(10),
                    payment_method INT NOT NULL,
                    status INT NOT NULL,
                    created_at_utc TIMESTAMP WITH TIME ZONE NOT NULL
                );

                CREATE TABLE IF NOT EXISTS order_items (
                    id UUID PRIMARY KEY,
                    order_id UUID NOT NULL REFERENCES orders(id) ON DELETE CASCADE,
                    product_id VARCHAR(100) NOT NULL,
                    product_name VARCHAR(200) NOT NULL,
                    unit_price NUMERIC(18,2) NOT NULL,
                    quantity INT NOT NULL
                );

                CREATE INDEX IF NOT EXISTS idx_orders_user_name ON orders(user_name);
                """;

            await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
            _logger.LogInformation("Order database tables and indexes initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Order database schema.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
