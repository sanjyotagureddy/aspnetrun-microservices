using Discount.Grpc.Entities;
using Discount.Grpc.Repositories.Interfaces;
using MediatR;

namespace Discount.Grpc.Application.Features.Discounts.Queries.GetDiscount;

internal sealed class GetDiscountQueryHandler(ICouponRepository repository) : IRequestHandler<GetDiscountQuery, Coupon?>
{
  private readonly ICouponRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

  public Task<Coupon?> Handle(GetDiscountQuery request, CancellationToken cancellationToken)
  {
    return _repository.GetByProductNameAsync(request.ProductName, cancellationToken);
  }
}