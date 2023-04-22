using System.Threading.Tasks;
using Discount.Grpc.Entities;

namespace Discount.Grpc.Repositories.Interfaces;

public interface IDiscountRepository
{
  Task<Coupon> GetDiscount(string productName);

  Task<bool> CreateDiscount(Coupon coupon);

  Task<bool> UpdateDiscount(Coupon coupon);

  Task<bool> DeleteDiscount(string productName);
}