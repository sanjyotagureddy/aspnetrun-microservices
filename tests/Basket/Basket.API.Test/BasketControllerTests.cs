using Basket.API.Application.Features.Basket.Commands.CheckoutBasket;
using Basket.API.Application.Features.Basket.Commands.DeleteBasket;
using Basket.API.Application.Features.Basket.Commands.UpdateBasket;
using Basket.API.Application.Features.Basket.Queries.GetBasket;
using Basket.API.Controllers;
using Basket.API.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Basket.API.Test;

public sealed class BasketControllerTests
{
    [Fact]
    public async Task GetBasket_ReturnsExistingBasket()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var controller = new BasketController(mediator.Object);
        var basket = new ShoppingCart("swn");

        mediator.Setup(m => m.Send(It.Is<GetBasketQuery>(query => query.UserName == "swn"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        ActionResult<ShoppingCart> result = await controller.GetBasket("swn", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(basket, okResult.Value);
        mediator.VerifyAll();
    }

    [Fact]
    public async Task GetBasket_FallsBackToEmptyBasket()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var controller = new BasketController(mediator.Object);

        mediator.Setup(m => m.Send(It.Is<GetBasketQuery>(query => query.UserName == "missing"), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShoppingCart)null!);

        ActionResult<ShoppingCart> result = await controller.GetBasket("missing", CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var basket = Assert.IsType<ShoppingCart>(okResult.Value);
        Assert.Equal("missing", basket.Id);
        mediator.VerifyAll();
    }

    [Fact]
    public async Task UpdateBasket_ReturnsUpdatedBasket()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var controller = new BasketController(mediator.Object);
        var basket = new ShoppingCart("swn");

        mediator.Setup(m => m.Send(It.Is<UpdateBasketCommand>(command => command.Basket == basket), It.IsAny<CancellationToken>()))
            .ReturnsAsync(basket);

        ActionResult<ShoppingCart> result = await controller.UpdateBasket(basket, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(basket, okResult.Value);
        mediator.VerifyAll();
    }

    [Fact]
    public async Task DeleteBasket_ReturnsOk()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var controller = new BasketController(mediator.Object);

        mediator.Setup(m => m.Send(It.Is<DeleteBasketCommand>(command => command.UserName == "swn"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        IActionResult result = await controller.DeleteBasket("swn", CancellationToken.None);

        Assert.IsType<OkResult>(result);
        mediator.VerifyAll();
    }

    [Fact]
    public async Task Checkout_ReturnsAccepted()
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var controller = new BasketController(mediator.Object);
        var command = new CheckoutBasketCommand { UserName = "swn", Cvv = "123" };

        mediator.Setup(m => m.Send(It.Is<CheckoutBasketCommand>(value => value == command), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        IActionResult result = await controller.Checkout(command, CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
        mediator.VerifyAll();
    }
}