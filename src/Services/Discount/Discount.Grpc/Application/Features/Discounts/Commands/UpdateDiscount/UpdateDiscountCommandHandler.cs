using Discount.Grpc.Entities;
using Discount.Grpc.Repositories.Interfaces;
using MediatR;

namespace Discount.Grpc.Application.Features.Discounts.Commands.UpdateDiscount;

internal sealed class UpdateDiscountCommandHandler(ICouponRepository repository) : IRequestHandler<UpdateDiscountCommand, Coupon>
{
  private readonly ICouponRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

  public async Task<Coupon> Handle(UpdateDiscountCommand request, CancellationToken cancellationToken)
  {
    await _repository.UpdateAsync(request.Coupon, cancellationToken);
    return request.Coupon;
  }
}