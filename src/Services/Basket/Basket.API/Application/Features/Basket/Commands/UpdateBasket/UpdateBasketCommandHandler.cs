using Basket.API.Application.Contracts.Infrastructure;
using Basket.API.Application.Contracts.Persistence;
using Basket.API.Domain.Entities;
using MediatR;

namespace Basket.API.Application.Features.Basket.Commands.UpdateBasket;

internal sealed class UpdateBasketCommandHandler(IBasketRepository repository, IDiscountService discountService)
  : IRequestHandler<UpdateBasketCommand, ShoppingCart>
{
  private readonly IDiscountService _discountService = discountService ?? throw new ArgumentNullException(nameof(discountService));
  private readonly IBasketRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

  public async Task<ShoppingCart> Handle(UpdateBasketCommand request, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(request);
    ArgumentNullException.ThrowIfNull(request.Basket);

    foreach (ShoppingCartItem item in request.Basket.Items)
    {
      decimal discount = await _discountService.GetDiscountAsync(item.ProductName, cancellationToken);
      item.Price -= discount;
    }

    return await _repository.UpdateBasketAsync(request.Basket, cancellationToken);
  }
}