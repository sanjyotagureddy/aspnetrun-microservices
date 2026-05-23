using Basket.API.Application.Features.Basket.Commands.CheckoutBasket;
using Basket.API.Application.Features.Basket.Commands.DeleteBasket;
using Basket.API.Application.Features.Basket.Commands.UpdateBasket;
using Basket.API.Application.Features.Basket.Queries.GetBasket;
using Basket.API.Domain.Entities;
using Basket.API.Endpoints;

using MediatR;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

using Moq;

using Xunit;

namespace Basket.API.Test;

public sealed class BasketEndpointsTests
{
    [Fact]
    public async Task GetBasket_ReturnsExistingBasket()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var basket = new ShoppingCart("swn");

        mediator.Setup(m => m.Send(It.Is<GetBasketQuery>(query => query.UserName == "swn"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        Ok<ShoppingCart> result = await BasketEndpoints.GetBasket(mediator.Object, "swn", CancellationToken.None);

        Assert.Same(basket, result.Value);
        mediator.VerifyAll();
    }

    [Fact]
    public async Task GetBasket_FallsBackToEmptyBasket()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        mediator.Setup(m => m.Send(It.Is<GetBasketQuery>(query => query.UserName == "missing"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShoppingCart)null!);

        Ok<ShoppingCart> result = await BasketEndpoints.GetBasket(mediator.Object, "missing", CancellationToken.None);

        Assert.Equal("missing", result.Value.Id);
        mediator.VerifyAll();
    }

    [Fact]
    public async Task UpdateBasket_ReturnsUpdatedBasket()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var basket = new ShoppingCart("swn");

        mediator.Setup(m => m.Send(It.Is<UpdateBasketCommand>(command => command.Basket == basket), It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        Ok<ShoppingCart> result = await BasketEndpoints.UpdateBasket(mediator.Object, basket, CancellationToken.None);

        Assert.Same(basket, result.Value);
        mediator.VerifyAll();
    }

    [Fact]
    public async Task DeleteBasket_ReturnsOk()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);

        mediator.Setup(m => m.Send(It.Is<DeleteBasketCommand>(command => command.UserName == "swn"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        Ok result = await BasketEndpoints.DeleteBasket(mediator.Object, "swn", CancellationToken.None);

        Assert.NotNull(result);
        mediator.VerifyAll();
    }

    [Fact]
    public async Task Checkout_ReturnsAccepted()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var command = new CheckoutBasketCommand { UserName = "swn", Cvv = "123" };

        mediator.Setup(m => m.Send(It.Is<CheckoutBasketCommand>(value => value == command), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        IResult result = await BasketEndpoints.Checkout(mediator.Object, command, CancellationToken.None);

        Assert.IsType<Accepted>(result);
        mediator.VerifyAll();
    }
}