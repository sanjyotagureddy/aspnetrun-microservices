using Basket.API.Application.Contracts.Persistence;
using Basket.API.Domain.Entities;
using MediatR;

namespace Basket.API.Application.Features.Basket.Queries.GetBasket;

internal sealed class GetBasketQueryHandler(IBasketRepository repository) : IRequestHandler<GetBasketQuery, ShoppingCart>
{
  private readonly IBasketRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

  public Task<ShoppingCart> Handle(GetBasketQuery request, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(request);
    return _repository.GetBasketAsync(request.UserName, cancellationToken);
  }
}