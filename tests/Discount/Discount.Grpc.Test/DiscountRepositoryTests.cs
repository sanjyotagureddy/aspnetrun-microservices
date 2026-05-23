using Discount.Grpc.Entities;
using Discount.Grpc.Repositories;
using NUnit.Framework;

namespace Discount.Grpc.Test;

[TestFixture]
public class DiscountRepositoryTests
{
  [Test]
  public async Task GetByProductNameAsync_ReturnsCouponWhenFound()
  {
    using var database = new TestDoubles.SqliteCouponDatabase();
    database.Reset();
    database.Seed(new Coupon { Id = 1, ProductName = "IPhone X", Description = "IPhone Discount", Amount = 150 });

    var repository = new CouponRepository(new TestDoubles.SqliteCouponConnectionFactory(database));

    var result = await repository.GetByProductNameAsync("IPhone X", CancellationToken.None);

    Assert.That(result, Is.Not.Null);
    Assert.That(result!.ProductName, Is.EqualTo("IPhone X"));
    Assert.That(result.Description, Is.EqualTo("IPhone Discount"));
    Assert.That(result.Amount, Is.EqualTo(150));
  }

  [Test]
  public async Task GetByProductNameAsync_ReturnsNullWhenMissing()
  {
    using var database = new TestDoubles.SqliteCouponDatabase();
    database.Reset();

    var repository = new CouponRepository(new TestDoubles.SqliteCouponConnectionFactory(database));

    var result = await repository.GetByProductNameAsync("Missing", CancellationToken.None);

    Assert.That(result, Is.Null);
  }

  [Test]
  public async Task CreateAsync_InsertsCoupon()
  {
    using var database = new TestDoubles.SqliteCouponDatabase();
    database.Reset();
    var repository = new CouponRepository(new TestDoubles.SqliteCouponConnectionFactory(database));
    var coupon = new Coupon { ProductName = "Pixel", Description = "Google Discount", Amount = 75 };

    var created = await repository.CreateAsync(coupon, CancellationToken.None);

    Assert.That(created, Is.True);
    Assert.That(database.Count(), Is.EqualTo(1));
    Assert.That(database.Find("Pixel"), Is.Not.Null);
  }

  [Test]
  public async Task UpdateAsync_UpdatesCoupon()
  {
    using var database = new TestDoubles.SqliteCouponDatabase();
    database.Reset();
    database.Seed(new Coupon { Id = 1, ProductName = "Pixel", Description = "Google Discount", Amount = 75 });
    var repository = new CouponRepository(new TestDoubles.SqliteCouponConnectionFactory(database));
    var coupon = new Coupon { Id = 1, ProductName = "Pixel 9", Description = "Updated", Amount = 80 };

    var updated = await repository.UpdateAsync(coupon, CancellationToken.None);

    Assert.That(updated, Is.True);
    var result = database.Find("Pixel 9");
    Assert.That(result, Is.Not.Null);
    Assert.That(result!.Description, Is.EqualTo("Updated"));
    Assert.That(result.Amount, Is.EqualTo(80));
  }

  [Test]
  public async Task DeleteAsync_RemovesCoupon()
  {
    using var database = new TestDoubles.SqliteCouponDatabase();
    database.Reset();
    database.Seed(new Coupon { Id = 1, ProductName = "Pixel", Description = "Google Discount", Amount = 75 });
    var repository = new CouponRepository(new TestDoubles.SqliteCouponConnectionFactory(database));

    var deleted = await repository.DeleteAsync("Pixel", CancellationToken.None);

    Assert.That(deleted, Is.True);
    Assert.That(database.Find("Pixel"), Is.Null);
  }
}