using SharedKernel.Domain.Entities;

namespace Basket.API.Domain.Entities;

public sealed class ShoppingCart : Entity<string>
{
  public ShoppingCart()
  {
  }

  public ShoppingCart(string userName)
  {
    Id = userName ?? throw new ArgumentNullException(nameof(userName));
  }

  public List<ShoppingCartItem> Items { get; set; } = new();

  public decimal TotalPrice => Items.Sum(item => item.Price * item.Quantity);
}