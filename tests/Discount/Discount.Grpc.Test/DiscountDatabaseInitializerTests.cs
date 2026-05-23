using Discount.Grpc.Repositories;
using NUnit.Framework;

namespace Discount.Grpc.Test;

[TestFixture]
public class DiscountDatabaseInitializerTests
{
  [Test]
  public void Initialize_CreatesAndSeedsCoupons()
  {
    using var database = new TestDoubles.SqliteCouponDatabase();
    var initializer = new DiscountDatabaseInitializer(new TestDoubles.SqliteCouponConnectionFactory(database));

    initializer.Initialize();

    Assert.That(database.Count(), Is.EqualTo(2));
    Assert.That(database.Find("IPhone X"), Is.Not.Null);
    Assert.That(database.Find("Samsung 10"), Is.Not.Null);
  }
}