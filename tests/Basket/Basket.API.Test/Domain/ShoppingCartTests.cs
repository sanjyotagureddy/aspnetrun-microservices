using Basket.API.Domain.Entities;
using Xunit;

namespace Basket.API.Test.Domain;

public sealed class ShoppingCartTests
{
    [Fact]
    public void Constructor_StoresUserNameAsId()
    {
        var cart = new ShoppingCart("alice");

        Assert.Equal("alice", cart.Id);
    }

    [Fact]
    public void TotalPrice_SumsAllItems()
    {
        var cart = new ShoppingCart("alice")
        {
            Items =
            [
                new ShoppingCartItem { Quantity = 2, Price = 10m },
                new ShoppingCartItem { Quantity = 3, Price = 5m }
            ]
        };

        Assert.Equal(35m, cart.TotalPrice);
    }

    [Fact]
    public void Constructor_ThrowsWhenUserNameIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => new ShoppingCart(null!));
    }
}