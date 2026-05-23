using Basket.API.Application.Contracts.Infrastructure;
using Discount.Grpc.Protos;

namespace Basket.API.Infrastructure.Services;

internal sealed class DiscountGrpcService(DiscountProtoService.DiscountProtoServiceClient discountProtoService) : IDiscountService
{
  private readonly DiscountProtoService.DiscountProtoServiceClient _discountProtoService = discountProtoService ?? throw new ArgumentNullException(nameof(discountProtoService));

  public async Task<decimal> GetDiscountAsync(string productName, CancellationToken cancellationToken = default)
  {
    var discountRequest = new GetDiscountRequest { ProductName = productName };
    CouponModel coupon = await _discountProtoService.GetDiscountAsync(discountRequest, cancellationToken: cancellationToken);
    return coupon.Amount;
  }
}