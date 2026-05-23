using System.Data.Common;
using Discount.Grpc.Entities;
using Discount.Grpc.Repositories.Interfaces;

namespace Discount.Grpc.Repositories;

internal sealed class CouponRepository(IDiscountConnectionFactory connectionFactory) : ICouponRepository
{
  private readonly IDiscountConnectionFactory _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));

  public async Task<Coupon?> GetByProductNameAsync(string productName, CancellationToken cancellationToken)
  {
    await using var connection = _connectionFactory.CreateConnection();
    await connection.OpenAsync(cancellationToken);

    await using var command = connection.CreateCommand();
    command.CommandText = "SELECT Id, ProductName, Description, Amount FROM Coupon WHERE ProductName = @ProductName";
    AddParameter(command, "@ProductName", productName);

    await using var reader = await command.ExecuteReaderAsync(cancellationToken);
    if (!await reader.ReadAsync(cancellationToken))
    {
      return null;
    }

    return MapCoupon(reader);
  }

  public async Task<bool> CreateAsync(Coupon coupon, CancellationToken cancellationToken)
  {
    await using var connection = _connectionFactory.CreateConnection();
    await connection.OpenAsync(cancellationToken);

    await using var command = connection.CreateCommand();
    command.CommandText = "INSERT INTO Coupon (ProductName, Description, Amount) VALUES (@ProductName, @Description, @Amount)";
    AddParameter(command, "@ProductName", coupon.ProductName);
    AddParameter(command, "@Description", coupon.Description);
    AddParameter(command, "@Amount", coupon.Amount);

    return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
  }

  public async Task<bool> UpdateAsync(Coupon coupon, CancellationToken cancellationToken)
  {
    await using var connection = _connectionFactory.CreateConnection();
    await connection.OpenAsync(cancellationToken);

    await using var command = connection.CreateCommand();
    command.CommandText = "UPDATE Coupon SET ProductName = @ProductName, Description = @Description, Amount = @Amount WHERE Id = @Id";
    AddParameter(command, "@ProductName", coupon.ProductName);
    AddParameter(command, "@Description", coupon.Description);
    AddParameter(command, "@Amount", coupon.Amount);
    AddParameter(command, "@Id", coupon.Id);

    return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
  }

  public async Task<bool> DeleteAsync(string productName, CancellationToken cancellationToken)
  {
    await using var connection = _connectionFactory.CreateConnection();
    await connection.OpenAsync(cancellationToken);

    await using var command = connection.CreateCommand();
    command.CommandText = "DELETE FROM Coupon WHERE ProductName = @ProductName";
    AddParameter(command, "@ProductName", productName);

    return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
  }

  private static Coupon MapCoupon(DbDataReader reader)
  {
    return new Coupon
    {
      Id = reader.GetInt32(reader.GetOrdinal("Id")),
      ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
      Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
      Amount = reader.GetInt32(reader.GetOrdinal("Amount"))
    };
  }

  private static void AddParameter(DbCommand command, string name, object? value)
  {
    var parameter = command.CreateParameter();
    parameter.ParameterName = name;
    parameter.Value = value ?? DBNull.Value;
    command.Parameters.Add(parameter);
  }
}