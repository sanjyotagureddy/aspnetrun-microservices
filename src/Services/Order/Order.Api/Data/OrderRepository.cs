using Dapper;
using Npgsql;
using Order.Api.Domain;

namespace Order.Api.Data;

public class OrderRepository : IOrderRepository
{
    private readonly NpgsqlDataSource _dataSource;

    public OrderRepository(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
    }

    public async Task<OrderEntity> CreateOrderAsync(OrderEntity order, CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string insertOrderSql = """
            INSERT INTO orders (id, user_name, total_price, first_name, last_name, email_address, address_line, country, state, zip_code, card_name, card_number, expiration, cvv, payment_method, status, created_at_utc)
            VALUES (@Id, @UserName, @TotalPrice, @FirstName, @LastName, @EmailAddress, @AddressLine, @Country, @State, @ZipCode, @CardName, @CardNumber, @Expiration, @CVV, @PaymentMethod, @Status, @CreatedAtUtc);
            """;

        await connection.ExecuteAsync(new CommandDefinition(insertOrderSql, new
        {
            order.Id,
            order.UserName,
            order.TotalPrice,
            order.FirstName,
            order.LastName,
            order.EmailAddress,
            order.AddressLine,
            order.Country,
            order.State,
            order.ZipCode,
            order.CardName,
            order.CardNumber,
            order.Expiration,
            order.CVV,
            order.PaymentMethod,
            Status = (int)order.Status,
            order.CreatedAtUtc
        }, transaction, cancellationToken: cancellationToken));

        foreach (var item in order.Items)
        {
            item.OrderId = order.Id;
            const string insertItemSql = """
                INSERT INTO order_items (id, order_id, product_id, product_name, unit_price, quantity)
                VALUES (@Id, @OrderId, @ProductId, @ProductName, @UnitPrice, @Quantity);
                """;

            await connection.ExecuteAsync(new CommandDefinition(insertItemSql, item, transaction, cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
        return order;
    }

    public async Task<IReadOnlyList<OrderEntity>> GetOrdersByUserNameAsync(string userName, CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT o.id, o.user_name AS UserName, o.total_price AS TotalPrice, o.first_name AS FirstName, o.last_name AS LastName,
                   o.email_address AS EmailAddress, o.address_line AS AddressLine, o.country AS Country, o.state AS State,
                   o.zip_code AS ZipCode, o.card_name AS CardName, o.card_number AS CardNumber, o.expiration AS Expiration,
                   o.cvv AS CVV, o.payment_method AS PaymentMethod, o.status AS Status, o.created_at_utc AS CreatedAtUtc,
                   i.id, i.order_id AS OrderId, i.product_id AS ProductId, i.product_name AS ProductName, i.unit_price AS UnitPrice, i.quantity AS Quantity
            FROM orders o
            LEFT JOIN order_items i ON o.id = i.order_id
            WHERE LOWER(o.user_name) = LOWER(@UserName)
            ORDER BY o.created_at_utc DESC;
            """;

        var orderDictionary = new Dictionary<Guid, OrderEntity>();

        await connection.QueryAsync<OrderEntity, OrderItem, OrderEntity>(
            new CommandDefinition(sql, new { UserName = userName }, cancellationToken: cancellationToken),
            (order, item) =>
            {
                if (!orderDictionary.TryGetValue(order.Id, out var existingOrder))
                {
                    existingOrder = order;
                    existingOrder.Items = new List<OrderItem>();
                    orderDictionary.Add(existingOrder.Id, existingOrder);
                }

                if (item is not null && item.Id != Guid.Empty)
                {
                    existingOrder.Items.Add(item);
                }

                return existingOrder;
            },
            splitOn: "id");

        return orderDictionary.Values.ToList();
    }

    public async Task<OrderEntity?> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        const string sql = """
            SELECT o.id, o.user_name AS UserName, o.total_price AS TotalPrice, o.first_name AS FirstName, o.last_name AS LastName,
                   o.email_address AS EmailAddress, o.address_line AS AddressLine, o.country AS Country, o.state AS State,
                   o.zip_code AS ZipCode, o.card_name AS CardName, o.card_number AS CardNumber, o.expiration AS Expiration,
                   o.cvv AS CVV, o.payment_method AS PaymentMethod, o.status AS Status, o.created_at_utc AS CreatedAtUtc,
                   i.id, i.order_id AS OrderId, i.product_id AS ProductId, i.product_name AS ProductName, i.unit_price AS UnitPrice, i.quantity AS Quantity
            FROM orders o
            LEFT JOIN order_items i ON o.id = i.order_id
            WHERE o.id = @Id;
            """;

        OrderEntity? result = null;

        await connection.QueryAsync<OrderEntity, OrderItem, OrderEntity>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken),
            (order, item) =>
            {
                result ??= order;
                result.Items ??= new List<OrderItem>();

                if (item is not null && item.Id != Guid.Empty)
                {
                    result.Items.Add(item);
                }

                return result;
            },
            splitOn: "id");

        return result;
    }

    public async Task<bool> CancelOrderAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken);

        const string sql = "UPDATE orders SET status = @Status WHERE id = @Id;";
        var affected = await connection.ExecuteAsync(new CommandDefinition(sql, new { Id = id, Status = (int)OrderStatus.Cancelled }, cancellationToken: cancellationToken));
        return affected > 0;
    }
}
