using Discount.Grpc.Entities;

namespace Discount.Grpc.Repositories.Interfaces;

public interface ICouponRepository
{
  Task<Coupon?> GetByProductNameAsync(string productName, CancellationToken cancellationToken);

  Task<bool> CreateAsync(Coupon coupon, CancellationToken cancellationToken);

  Task<bool> UpdateAsync(Coupon coupon, CancellationToken cancellationToken);

  Task<bool> DeleteAsync(string productName, CancellationToken cancellationToken);
}