using Discount.Grpc.Domain;

namespace Discount.Grpc.Data;

public interface IDiscountRepository
{
    Task<Coupon> GetDiscountAsync(string productName, CancellationToken cancellationToken = default);
    Task<bool> CreateDiscountAsync(Coupon coupon, CancellationToken cancellationToken = default);
    Task<bool> UpdateDiscountAsync(Coupon coupon, CancellationToken cancellationToken = default);
    Task<bool> DeleteDiscountAsync(string productName, CancellationToken cancellationToken = default);
}
