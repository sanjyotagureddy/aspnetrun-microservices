using System.Threading.Tasks;
using Basket.API.Entities;

namespace Basket.API.Repositories.Interfaces;

public interface IBasketRepository
{
  Task<ShoppingCart> GetBasket(string userName);

  Task<ShoppingCart> UpdateBasket(ShoppingCart basket);

  Task DeleteBasket(string userName);
}