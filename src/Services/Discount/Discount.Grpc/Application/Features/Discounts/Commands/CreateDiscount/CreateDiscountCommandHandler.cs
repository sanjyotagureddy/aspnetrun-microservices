using Discount.Grpc.Entities;
using Discount.Grpc.Repositories.Interfaces;
using MediatR;

namespace Discount.Grpc.Application.Features.Discounts.Commands.CreateDiscount;

internal sealed class CreateDiscountCommandHandler(ICouponRepository repository) : IRequestHandler<CreateDiscountCommand, Coupon>
{
  private readonly ICouponRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

  public async Task<Coupon> Handle(CreateDiscountCommand request, CancellationToken cancellationToken)
  {
    await _repository.CreateAsync(request.Coupon, cancellationToken);
    return request.Coupon;
  }
}