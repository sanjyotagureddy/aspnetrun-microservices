using Basket.API.Domain.Entities;

namespace Basket.API.Application.Contracts.Persistence;

public interface IBasketRepository
{
  Task<ShoppingCart> GetBasketAsync(string userName, CancellationToken cancellationToken = default);

  Task<ShoppingCart> UpdateBasketAsync(ShoppingCart basket, CancellationToken cancellationToken = default);

  Task DeleteBasketAsync(string userName, CancellationToken cancellationToken = default);
}