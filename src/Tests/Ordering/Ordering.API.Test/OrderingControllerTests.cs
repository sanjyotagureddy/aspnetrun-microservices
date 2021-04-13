using System.Collections.Generic;
using System.Net;
using System.Threading;
using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Ordering.API.Controllers;
using Ordering.Application.Contracts.Infrastructure;
using Ordering.Application.Contracts.Persistence;
using Ordering.Application.Exceptions;
using Ordering.Application.Features.Orders.Commands.DeleteCommand;
using Ordering.Application.Features.Orders.Commands.UpdateOrder;
using Ordering.Application.Features.Orders.Queries.GetOrdersList;
using Ordering.Application.Mappings;
using Ordering.Domain.Entities;

namespace Ordering.API.Test
{
    public class Tests
    {
        private Mock<IMediator> _mediator;
        private IMapper _mapper;
        private Mock<ILogger<DeleteOrderCommandHandler>> _deleteCommandLogger;
        private Mock<ILogger<UpdateOrderCommandHandler>> _updateCommandLogger;
        private Mock<IEmailService> _emailMock;
        private readonly Mock<IOrderRepository> _orderRepoMock;
        private readonly Mock<IMediator> _mockMediatr;
        private OrderController _controller;

        private GetOrdersListQueryHandler _getOrdersListQueryHandler;
        private DeleteOrderCommandHandler _deleteOrderCommandHandler;
        private UpdateOrderCommandHandler _updateOrderCommandHandler;

        private readonly Order _order;
        private readonly List<Order> _orders;

        private readonly UpdateOrderCommand _updateOrderCommand;

        public Tests()
        {
            _mediator = new Mock<IMediator>();
            _emailMock = new Mock<IEmailService>();
            _orderRepoMock = new Mock<IOrderRepository>();
            _mockMediatr = new Mock<IMediator>();

            _order = new Order() { UserName = "swn" };
            _orders = new List<Order>() {
                new() {
                    UserName = "swn"
                }
            };

            _updateOrderCommand = new UpdateOrderCommand()
            {
                UserName = "swn",
                TotalPrice = 0
            };
        }

        [SetUp]
        public void Setup()
        {
            //Mapper
            var mapperConfig = new MapperConfiguration(mc =>
            {
                mc.AddProfile(new MappingProfile());
            });
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
                        new CancellationToken()));

            var orders = _controller.GetOrdersByUserName(userName);
            if (orders.Result.Result is OkObjectResult okResult)
            {
                Assert.NotNull(orders.Result);
                Assert.AreEqual((int)HttpStatusCode.OK, okResult.StatusCode);
            }
            else
                Assert.Fail();
        }

        [Test]
        public void UpdateOrder_ReturnNoContent()
        {
            _updateCommandLogger = new Mock<ILogger<UpdateOrderCommandHandler>>();
            _orderRepoMock.Setup(p => p.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(_order);

            //GET QueryHandler
            _updateOrderCommandHandler = new UpdateOrderCommandHandler(_orderRepoMock.Object, _mapper, _updateCommandLogger.Object);
            _mockMediatr.Setup(m => m.Send(It.IsAny<UpdateOrderCommand>(), It.IsAny<CancellationToken>()))
                .Returns(async () =>
                    await _updateOrderCommandHandler.Handle(new UpdateOrderCommand(),
                        new CancellationToken()));


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
            _deleteOrderCommandHandler = new DeleteOrderCommandHandler(_orderRepoMock.Object, _mapper, _deleteCommandLogger.Object);
            _mockMediatr.Setup(m => m.Send(It.IsAny<DeleteOrderCommand>(), It.IsAny<CancellationToken>()))
                .Returns(async () =>
                    await _deleteOrderCommandHandler.Handle(new DeleteOrderCommand(),
                        new CancellationToken()));


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
                        new CancellationToken()));

            var orders = _controller.GetOrdersByUserName(userName);
            if (orders.Result.Result is OkObjectResult okResult)
            {
                Assert.IsNull(orders.Result.Value);
            }
            else
                Assert.Fail();
        }
    }
}

