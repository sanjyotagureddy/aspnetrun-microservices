using System.Net;

using AutoMapper;

using MediatR;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Moq;

using NUnit.Framework;

using Ordering.API.Controllers;
using Ordering.Application.Contracts.Infrastructure;
using Ordering.Application.Contracts.Persistence;
using Ordering.Application.Features.Orders.Commands.DeleteCommand;
using Ordering.Application.Features.Orders.Commands.UpdateOrder;
using Ordering.Application.Features.Orders.Queries.GetOrdersList;
using Ordering.Application.Mappings;
using Ordering.Domain.Entities;

namespace Ordering.API.Test;

public class Tests
{
    private readonly Mock<IMediator> _mockMediatr = new();

    private readonly Order _order = new() { UserName = "swn" };
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly List<Order> _orders = new()
  {
    new()
    {
      UserName = "swn"
    }
  };

    private readonly UpdateOrderCommand _updateOrderCommand = new()
    {
        UserName = "swn",
        TotalPrice = 0
    };
    private OrderController _controller;
    private Mock<ILogger<DeleteOrderCommandHandler>> _deleteCommandLogger;
    private DeleteOrderCommandHandler _deleteOrderCommandHandler;
    private Mock<IEmailService> _emailMock = new();

    private GetOrdersListQueryHandler _getOrdersListQueryHandler;
    private IMapper _mapper;
    private Mock<IMediator> _mediator = new();
    private Mock<ILogger<UpdateOrderCommandHandler>> _updateCommandLogger;
    private UpdateOrderCommandHandler _updateOrderCommandHandler;

    [SetUp]
    public void Setup()
    {
        //Mapper
        var mapperConfig = new MapperConfiguration(mc => { mc.AddProfile(new MappingProfile()); }, new LoggerFactory());
        _mapper = mapperConfig.CreateMapper();

        //Controller
        _controller = new OrderController(_mockMediatr.Object);
        _deleteCommandLogger = new Mock<ILogger<DeleteOrderCommandHandler>>();
    }

    [Test]
    [TestCase("swn")]
    public void GetOrdersByUserName(string userName)
    {
        _orderRepoMock.Setup(p => p.GetOrdersByUserName(userName)).ReturnsAsync(_orders);
        //GET QueryHandler
        _getOrdersListQueryHandler = new GetOrdersListQueryHandler(_orderRepoMock.Object, _mapper);
        _mockMediatr.Setup(m => m.Send(It.IsAny<GetOrdersListQuery>(), It.IsAny<CancellationToken>()))
          .Returns(async () =>
            await _getOrdersListQueryHandler.Handle(new GetOrdersListQuery(userName),
              CancellationToken.None));

        var orders = _controller.GetOrdersByUserName(userName);
        if (orders.Result.Result is OkObjectResult okResult)
        {
            Assert.NotNull(orders.Result);
            Assert.AreEqual((int)HttpStatusCode.OK, okResult.StatusCode);
        }
        else
        {
            Assert.Fail();
        }
    }

    [Test]
    public void UpdateOrder_ReturnNoContent()
    {
        _updateCommandLogger = new Mock<ILogger<UpdateOrderCommandHandler>>();
        _orderRepoMock.Setup(p => p.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(_order);

        //GET QueryHandler
        _updateOrderCommandHandler =
          new UpdateOrderCommandHandler(_orderRepoMock.Object, _mapper, _updateCommandLogger.Object);
        _mockMediatr.Setup(m => m.Send(It.IsAny<UpdateOrderCommand>(), It.IsAny<CancellationToken>()))
          .Returns(async () =>
            await _updateOrderCommandHandler.Handle(new UpdateOrderCommand(),
              CancellationToken.None));

        _orderRepoMock.Setup(p => p.UpdateAsync(It.IsAny<Order>()));
        var orders = _controller.UpdateOrder(_updateOrderCommand);
        if (orders.Result is NoContentResult okObject)
            Assert.Pass();
        else
            Assert.Fail();
    }

    [Test]
    [TestCase(2)]
    public void DeleteOrder_ReturnNoContent(int id)
    {
        _deleteCommandLogger = new Mock<ILogger<DeleteOrderCommandHandler>>();
        _orderRepoMock.Setup(p => p.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(_order);
        //GET QueryHandler
        _deleteOrderCommandHandler =
          new DeleteOrderCommandHandler(_orderRepoMock.Object, _mapper, _deleteCommandLogger.Object);
        _mockMediatr.Setup(m => m.Send(It.IsAny<DeleteOrderCommand>(), It.IsAny<CancellationToken>()))
          .Returns(async () =>
            await _deleteOrderCommandHandler.Handle(new DeleteOrderCommand(),
              CancellationToken.None));

        _orderRepoMock.Setup(p => p.DeleteAsync(It.IsAny<Order>()));

        var orders = _controller.DeleteOrder(id);
        if (orders.Result is NoContentResult okObject)
            Assert.AreEqual(okObject.StatusCode, (int)HttpStatusCode.NoContent);
        else
            Assert.Fail();
    }

    [Test]
    [TestCase("abc")]
    public void GetOrdersByUserName_NotFound(string userName)
    {
        _orderRepoMock.Setup(p => p.GetOrdersByUserName(userName)).ReturnsAsync((IEnumerable<Order>)null);
        //GET QueryHandler
        _getOrdersListQueryHandler = new GetOrdersListQueryHandler(_orderRepoMock.Object, _mapper);
        _mockMediatr.Setup(m => m.Send(It.IsAny<GetOrdersListQuery>(), It.IsAny<CancellationToken>()))
          .Returns(async () =>
            await _getOrdersListQueryHandler.Handle(new GetOrdersListQuery(userName),
              CancellationToken.None));

        var orders = _controller.GetOrdersByUserName(userName);
        if (orders.Result.Result is OkObjectResult okResult)
            Assert.IsNull(orders.Result.Value);
        else
            Assert.Fail();
    }
}