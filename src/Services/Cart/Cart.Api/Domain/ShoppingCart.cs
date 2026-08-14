namespace Cart.Api.Domain;

public class ShoppingCart
{
    public string UserName { get; set; } = string.Empty;
    public List<ShoppingCartItem> Items { get; set; } = new();

    public ShoppingCart()
    {
    }

    public ShoppingCart(string userName)
    {
        UserName = userName;
    }

    public decimal TotalPrice
    {
        get
        {
            decimal total = 0;
            foreach (var item in Items)
            {
                total += item.Price * item.Quantity;
            }
            return total;
        }
    }
}
