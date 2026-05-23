using Discount.Grpc.Entities;
using Discount.Grpc.Repositories.Interfaces;
using Microsoft.Data.Sqlite;
using System.Data.Common;

namespace Discount.Grpc.Test.TestDoubles;

internal sealed class SqliteCouponDatabase : IDisposable
{
  private readonly SqliteConnection _keeperConnection;

  public SqliteCouponDatabase()
  {
    ConnectionString = $"Data Source=file:{Guid.NewGuid():N}?mode=memory&cache=shared";
    _keeperConnection = new SqliteConnection(ConnectionString);
    _keeperConnection.Open();
  }

  public string ConnectionString { get; }

  public DbConnection CreateConnection()
  {
    return new SqliteConnection(ConnectionString);
  }

  public void Reset()
  {
    Execute("DROP TABLE IF EXISTS Coupon;");
    Execute("CREATE TABLE Coupon (Id INTEGER PRIMARY KEY NOT NULL, ProductName TEXT NOT NULL, Description TEXT, Amount INTEGER NOT NULL);");
  }

  public void Seed(Coupon coupon)
  {
    using var command = _keeperConnection.CreateCommand();
    command.CommandText = "INSERT INTO Coupon (Id, ProductName, Description, Amount) VALUES (@Id, @ProductName, @Description, @Amount)";
    AddParameter(command, "@Id", coupon.Id);
    AddParameter(command, "@ProductName", coupon.ProductName);
    AddParameter(command, "@Description", coupon.Description);
    AddParameter(command, "@Amount", coupon.Amount);
    command.ExecuteNonQuery();
  }

  public Coupon? Find(string productName)
  {
    using var command = _keeperConnection.CreateCommand();
    command.CommandText = "SELECT Id, ProductName, Description, Amount FROM Coupon WHERE ProductName = @ProductName";
    AddParameter(command, "@ProductName", productName);

    using var reader = command.ExecuteReader();
    if (!reader.Read())
    {
      return null;
    }

    return new Coupon
    {
      Id = reader.GetInt32(reader.GetOrdinal("Id")),
      ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
      Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
      Amount = reader.GetInt32(reader.GetOrdinal("Amount"))
    };
  }

  public int Count()
  {
    using var command = _keeperConnection.CreateCommand();
    command.CommandText = "SELECT COUNT(*) FROM Coupon";
    return Convert.ToInt32(command.ExecuteScalar());
  }

  private void Execute(string sql)
  {
    using var command = _keeperConnection.CreateCommand();
    command.CommandText = sql;
    command.ExecuteNonQuery();
  }

  private static void AddParameter(DbCommand command, string name, object? value)
  {
    var parameter = command.CreateParameter();
    parameter.ParameterName = name;
    parameter.Value = value ?? DBNull.Value;
    command.Parameters.Add(parameter);
  }

  public void Dispose()
  {
    _keeperConnection.Dispose();
  }
}

internal sealed class SqliteCouponConnectionFactory(SqliteCouponDatabase database) : IDiscountConnectionFactory
{
  public DbConnection CreateConnection()
  {
    return database.CreateConnection();
  }
}