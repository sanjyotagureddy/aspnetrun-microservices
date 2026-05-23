namespace Basket.API.Domain.Entities;

public sealed class ShoppingCartItem
{
  public int Quantity { get; set; }

  public string Color { get; set; } = string.Empty;

  public decimal Price { get; set; }

  public string ProductId { get; set; } = string.Empty;

  public string ProductName { get; set; } = string.Empty;
}