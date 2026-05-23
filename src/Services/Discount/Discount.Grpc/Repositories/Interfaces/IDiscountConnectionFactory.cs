using System.Data.Common;

namespace Discount.Grpc.Repositories.Interfaces;

public interface IDiscountConnectionFactory
{
  DbConnection CreateConnection();
}