using Discount.Grpc.Repositories.Interfaces;

namespace Discount.Grpc.Repositories;

internal sealed class DiscountDatabaseInitializer(IDiscountConnectionFactory connectionFactory) : IDiscountDatabaseInitializer
{
  private readonly IDiscountConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

  public void Initialize()
  {
    using var connection = _connectionFactory.CreateConnection();
    connection.Open();

    using var command = connection.CreateCommand();
    command.CommandText = "DROP TABLE IF EXISTS Coupon";
    command.ExecuteNonQuery();

    command.CommandText = @"CREATE TABLE Coupon(
                                    Id              INT PRIMARY KEY NOT NULL,
                                    ProductName     VARCHAR(24) NOT NULL,
                                    Description     TEXT,
                                    Amount          INT NOT NULL
                                  );";
    command.ExecuteNonQuery();

    command.CommandText = @"INSERT INTO Coupon (Id, ProductName, Description, Amount) VALUES (1, 'IPhone X', 'IPhone Discount', 150);";
    command.ExecuteNonQuery();

    command.CommandText = @"INSERT INTO Coupon (Id, ProductName, Description, Amount) VALUES (2, 'Samsung 10', 'Samsung Discount', 100);";
    command.ExecuteNonQuery();
  }
}