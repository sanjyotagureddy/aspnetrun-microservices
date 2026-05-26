using Discount.Grpc.Entities;
using Discount.Grpc.Repositories;
using Xunit;

namespace Discount.Grpc.Test;

public class DiscountRepositoryTests
{
  [Fact]
  public async Task GetByProductNameAsync_ReturnsCouponWhenFound()
  {
    using var database = new TestDoubles.SqliteCouponDatabase();
    database.Reset();
    database.Seed(new Coupon { Id = 1, ProductName = "IPhone X", Description = "IPhone Discount", Amount = 150 });

    var repository = new CouponRepository(new TestDoubles.SqliteCouponConnectionFactory(database));

    var result = await repository.GetByProductNameAsync("IPhone X", CancellationToken.None);

    Assert.NotNull(result);
    Assert.Equal("IPhone X", result!.ProductName);
    Assert.Equal("IPhone Discount", result.Description);
    Assert.Equal(150, result.Amount);
  }

  [Fact]
  public async Task GetByProductNameAsync_ReturnsNullWhenMissing()
  {
    using var database = new TestDoubles.SqliteCouponDatabase();
    database.Reset();

    var repository = new CouponRepository(new TestDoubles.SqliteCouponConnectionFactory(database));

    var result = await repository.GetByProductNameAsync("Missing", CancellationToken.None);

    Assert.Null(result);
  }

  [Fact]
  public async Task CreateAsync_InsertsCoupon()
  {
    using var database = new TestDoubles.SqliteCouponDatabase();
    database.Reset();
    var repository = new CouponRepository(new TestDoubles.SqliteCouponConnectionFactory(database));
    var coupon = new Coupon { ProductName = "Pixel", Description = "Google Discount", Amount = 75 };

    var created = await repository.CreateAsync(coupon, CancellationToken.None);

    Assert.True(created);
    Assert.Equal(1, database.Count());
    Assert.NotNull(database.Find("Pixel"));
  }

  [Fact]
  public async Task UpdateAsync_UpdatesCoupon()
  {
    using var database = new TestDoubles.SqliteCouponDatabase();
    database.Reset();
    database.Seed(new Coupon { Id = 1, ProductName = "Pixel", Description = "Google Discount", Amount = 75 });
    var repository = new CouponRepository(new TestDoubles.SqliteCouponConnectionFactory(database));
    var coupon = new Coupon { Id = 1, ProductName = "Pixel 9", Description = "Updated", Amount = 80 };

    var updated = await repository.UpdateAsync(coupon, CancellationToken.None);

    Assert.True(updated);
    var result = database.Find("Pixel 9");
    Assert.NotNull(result);
    Assert.Equal("Updated", result!.Description);
    Assert.Equal(80, result.Amount);
  }

  [Fact]
  public async Task DeleteAsync_RemovesCoupon()
  {
    using var database = new TestDoubles.SqliteCouponDatabase();
    database.Reset();
    database.Seed(new Coupon { Id = 1, ProductName = "Pixel", Description = "Google Discount", Amount = 75 });
    var repository = new CouponRepository(new TestDoubles.SqliteCouponConnectionFactory(database));

    var deleted = await repository.DeleteAsync("Pixel", CancellationToken.None);

    Assert.True(deleted);
    Assert.Null(database.Find("Pixel"));
  }
}