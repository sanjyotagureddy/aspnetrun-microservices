using Cart.Api.Data;
using Cart.Api.Domain;
using Cart.Api.Events;
using Cart.Api.Features.Cart.Checkout;
using Cart.Api.Features.Cart.DeleteCart;
using Cart.Api.Features.Cart.GetCart;
using Cart.Api.Features.Cart.UpdateCart;
using Common.SharedKernel.Messaging;
using Discount.Grpc.Protos;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Cart.Api.Tests;

public class CartFeatureTests
{
    private readonly ICartRepository _cartRepository;
    private readonly IDistributedCache _cache;

    public CartFeatureTests()
    {
        var opts = Options.Create(new MemoryDistributedCacheOptions());
        _cache = new MemoryDistributedCache(opts);
        _cartRepository = new CartRepository(_cache);
    }

    [Fact]
    public async Task GetCart_ShouldReturnEmptyCart_WhenUserHasNoCart()
    {
        // Arrange
        var handler = new GetCartHandler(_cartRepository);
        var query = new GetCartQuery("sanjyot");

        // Act
        var cart = await handler.Handle(query, CancellationToken.None);

        // Assert
        cart.Should().NotBeNull();
        cart.UserName.Should().Be("sanjyot");
        cart.Items.Should().BeEmpty();
        cart.TotalPrice.Should().Be(0);
    }

    [Fact]
    public async Task UpdateCart_ShouldStoreCart_AndCalculateTotalPrice()
    {
        // Arrange
        var mockDiscountClient = Substitute.For<DiscountProtoService.DiscountProtoServiceClient>();
        var handler = new UpdateCartHandler(_cartRepository, mockDiscountClient);

        var cart = new ShoppingCart("sanjyot")
        {
            Items = new List<ShoppingCartItem>
            {
                new ShoppingCartItem { ProductId = "p1", ProductName = "Widget", Price = 100, Quantity = 2, Color = "Red" }
            }
        };

        var command = new UpdateCartCommand(cart);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.UserName.Should().Be("sanjyot");
        result.Items.Should().HaveCount(1);
        result.TotalPrice.Should().Be(200);
    }

    [Fact]
    public async Task DeleteCart_ShouldRemoveCartFromStorage()
    {
        // Arrange
        var cart = new ShoppingCart("sanjyot")
        {
            Items = new List<ShoppingCartItem>
            {
                new ShoppingCartItem { ProductId = "p1", ProductName = "Widget", Price = 50, Quantity = 1 }
            }
        };
        await _cartRepository.UpdateCartAsync(cart);

        var handler = new DeleteCartHandler(_cartRepository);
        var command = new DeleteCartCommand("sanjyot");

        // Act
        var deleted = await handler.Handle(command, CancellationToken.None);
        var fetchAfterDelete = await _cartRepository.GetCartAsync("sanjyot");

        // Assert
        deleted.Should().BeTrue();
        fetchAfterDelete.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckoutCart_ShouldPublishEventAndDeleteCart_WhenCartHasItems()
    {
        // Arrange
        var cart = new ShoppingCart("sanjyot")
        {
            Items = new List<ShoppingCartItem>
            {
                new ShoppingCartItem { ProductId = "p1", ProductName = "Widget", Price = 100, Quantity = 1 }
            }
        };
        await _cartRepository.UpdateCartAsync(cart);

        var mockMessageBus = Substitute.For<IMessageBus>();
        var timeProvider = TimeProvider.System;
        var handler = new CheckoutCartHandler(_cartRepository, mockMessageBus, timeProvider);

        var dto = new CartCheckoutDto(
            "sanjyot",
            "Sanjyota",
            "Gureddy",
            "sanjyota@example.com",
            "123 Main St",
            "USA",
            "NY",
            "10001",
            "Sanjyota G",
            "4111111111111111",
            "12/28",
            "123",
            1);

        var command = new CheckoutCartCommand(dto);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);
        var cartAfterCheckout = await _cartRepository.GetCartAsync("sanjyot");

        // Assert
        result.Should().BeTrue();
        await mockMessageBus.Received(1).PublishAsync(
            Arg.Is<string>(topic => topic == BasketCheckoutIntegrationEvent.Topic),
            Arg.Is<BasketCheckoutIntegrationEvent>(evt => evt.UserName == "sanjyot" && evt.TotalPrice == 100),
            Arg.Any<CancellationToken>());

        cartAfterCheckout.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckoutCart_ShouldReturnFalse_WhenCartIsEmpty()
    {
        // Arrange
        var mockMessageBus = Substitute.For<IMessageBus>();
        var timeProvider = TimeProvider.System;
        var handler = new CheckoutCartHandler(_cartRepository, mockMessageBus, timeProvider);

        var dto = new CartCheckoutDto(
            "emptyuser",
            "First",
            "Last",
            "email@example.com",
            "Address",
            "Country",
            "State",
            "Zip",
            "Card",
            "Number",
            "Exp",
            "CVV",
            1);

        var command = new CheckoutCartCommand(dto);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        await mockMessageBus.DidNotReceiveWithAnyArgs().PublishAsync<BasketCheckoutIntegrationEvent>(default!, default!, default!);
    }
}
