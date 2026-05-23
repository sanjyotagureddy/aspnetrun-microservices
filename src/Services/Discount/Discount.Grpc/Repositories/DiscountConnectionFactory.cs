using System.Data.Common;
using Discount.Grpc.Repositories.Interfaces;
using Npgsql;

namespace Discount.Grpc.Repositories;

internal sealed class DiscountConnectionFactory(IConfiguration configuration) : IDiscountConnectionFactory
{
  private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

  public DbConnection CreateConnection()
  {
    var connectionString = _configuration.GetValue<string>("DatabaseSettings:ConnectionString");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
      throw new InvalidOperationException("DatabaseSettings:ConnectionString is required.");
    }

    return new NpgsqlConnection(connectionString);
  }
}