using Basket.API.Application.Contracts.Infrastructure;
using Basket.API.Application.Contracts.Persistence;
using Basket.API.Application.Features.Basket.Commands.CheckoutBasket;
using Basket.API.Application.Features.Basket.Commands.DeleteBasket;
using Basket.API.Application.Features.Basket.Commands.UpdateBasket;
using Basket.API.Application.Features.Basket.Queries.GetBasket;
using Basket.API.Domain.Entities;
using MediatR;
using Moq;
using Shared.Messaging.Events;
using SharedKernel.Exceptions;
using Xunit;

namespace Basket.API.Test.Application;

public sealed class BasketHandlersTests
{
    [Fact]
    public async Task GetBasketQueryHandler_ReturnsBasketFromRepository()
    {
        var repository = new Mock<IBasketRepository>(MockBehavior.Strict);
        var basket = new ShoppingCart("alice");
        repository.Setup(r => r.GetBasketAsync("alice", It.IsAny<CancellationToken>())).ReturnsAsync(basket);
        var handler = new GetBasketQueryHandler(repository.Object);

        ShoppingCart result = await handler.Handle(new GetBasketQuery("alice"), CancellationToken.None);

        Assert.Same(basket, result);
        repository.VerifyAll();
    }

    [Fact]
    public async Task UpdateBasketCommandHandler_ResolvesDiscountsAndPersistsBasket()
    {
        var repository = new Mock<IBasketRepository>();
        var discountService = new Mock<IDiscountService>();
        var basket = new ShoppingCart("alice")
        {
            Items = [new ShoppingCartItem { ProductName = "book", Price = 20m, Quantity = 1 }]
        };

        discountService.Setup(service => service.GetDiscountAsync("book", It.IsAny<CancellationToken>())).ReturnsAsync(5m);
        repository.Setup(service => service.UpdateBasketAsync(basket, It.IsAny<CancellationToken>())).ReturnsAsync(basket);

        var handler = new UpdateBasketCommandHandler(repository.Object, discountService.Object);

        ShoppingCart result = await handler.Handle(new UpdateBasketCommand(basket), CancellationToken.None);

        Assert.Equal(15m, result.Items[0].Price);
        repository.Verify(service => service.UpdateBasketAsync(basket, It.IsAny<CancellationToken>()), Times.Once);
        discountService.VerifyAll();
    }

    [Fact]
    public async Task DeleteBasketCommandHandler_DeletesBasket()
    {
        var repository = new Mock<IBasketRepository>();
        repository.Setup(service => service.DeleteBasketAsync("alice", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var handler = new DeleteBasketCommandHandler(repository.Object);

        Unit result = await handler.Handle(new DeleteBasketCommand("alice"), CancellationToken.None);

        Assert.Equal(Unit.Value, result);
        repository.VerifyAll();
    }

    [Fact]
    public async Task CheckoutBasketCommandHandler_PublishesEventAndDeletesBasket()
    {
        var repository = new Mock<IBasketRepository>();
        var publisher = new Mock<IBasketCheckoutPublisher>();
        var basket = new ShoppingCart("alice")
        {
            Items = [new ShoppingCartItem { ProductName = "book", Price = 10m, Quantity = 2 }]
        };

        BasketCheckoutEvent publishedEvent = null!;
        repository.Setup(service => service.GetBasketAsync("alice", It.IsAny<CancellationToken>())).ReturnsAsync(basket);
        repository.Setup(service => service.DeleteBasketAsync("alice", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        publisher.Setup(service => service.PublishAsync(It.IsAny<BasketCheckoutEvent>(), It.IsAny<CancellationToken>()))
            .Callback<BasketCheckoutEvent, CancellationToken>((eventMessage, _) => publishedEvent = eventMessage)
            .Returns(Task.CompletedTask);

        var handler = new CheckoutBasketCommandHandler(repository.Object, publisher.Object);

        Unit result = await handler.Handle(new CheckoutBasketCommand
        {
            UserName = "alice",
            FirstName = "A",
            LastName = "User",
            EmailAddress = "alice@example.com",
            AddressLine = "1 Main St",
            Country = "US",
            State = "WA",
            ZipCode = "98101",
            CardName = "Alice User",
            CardNumber = "4111111111111111",
            Expiration = "12/28",
            Cvv = "123",
            PaymentMethod = 1
        }, CancellationToken.None);

        Assert.Equal(Unit.Value, result);
        Assert.NotNull(publishedEvent);
        Assert.Equal(20m, publishedEvent.TotalPrice);
        Assert.Equal("alice", publishedEvent.UserName);
        repository.VerifyAll();
        publisher.VerifyAll();
    }

    [Fact]
    public async Task CheckoutBasketCommandHandler_ThrowsWhenBasketMissing()
    {
        var repository = new Mock<IBasketRepository>();
        var publisher = new Mock<IBasketCheckoutPublisher>();
        repository.Setup(service => service.GetBasketAsync("missing", It.IsAny<CancellationToken>())).ReturnsAsync((ShoppingCart)null!);
        var handler = new CheckoutBasketCommandHandler(repository.Object, publisher.Object);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new CheckoutBasketCommand { UserName = "missing" }, CancellationToken.None));
    }
}