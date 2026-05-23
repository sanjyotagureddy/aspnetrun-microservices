using Discount.Grpc.Repositories.Interfaces;
using MediatR;

namespace Discount.Grpc.Application.Features.Discounts.Commands.DeleteDiscount;

internal sealed class DeleteDiscountCommandHandler(ICouponRepository repository) : IRequestHandler<DeleteDiscountCommand, bool>
{
  private readonly ICouponRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

  public Task<bool> Handle(DeleteDiscountCommand request, CancellationToken cancellationToken)
  {
    return _repository.DeleteAsync(request.ProductName, cancellationToken);
  }
}