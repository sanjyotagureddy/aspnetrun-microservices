using Discount.Grpc.Repositories;
using Xunit;

namespace Discount.Grpc.Test;

public class DiscountDatabaseInitializerTests
{
  [Fact]
  public void Initialize_CreatesAndSeedsCoupons()
  {
    using var database = new TestDoubles.SqliteCouponDatabase();
    var initializer = new DiscountDatabaseInitializer(new TestDoubles.SqliteCouponConnectionFactory(database));

    initializer.Initialize();

    Assert.Equal(2, database.Count());
    Assert.NotNull(database.Find("IPhone X"));
    Assert.NotNull(database.Find("Samsung 10"));
  }
}