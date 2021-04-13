using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
using Ordering.Application.Features.Orders.Queries.GetOrdersList;
using Ordering.Application.Mappings;
using Ordering.Domain.Entities;

namespace Ordering.API.Test
{
    public class Tests
    {
        private Mock<IMediator> _mediator;
        private IMapper _mapper;
        private Mock<ILogger> _logger;
        private Mock<IEmailService> _emailMock;
        private Mock<IOrderRepository> _orderRepoMock;
        private Mock<IMediator> _mockMediatr;
        private GetOrdersListQueryHandler _getOrdersListQueryHandler;
        private OrderController _controller;



        private readonly List<Order> _orders;
        private readonly List<OrdersVm> _orderVms;

        public Tests()
        {
            _orders = new List<Order>()
            {
                new()
                {
                    UserName = "swn"
                }
            };

            _orderVms = new List<OrdersVm>()
            {
                new()
                {
                    UserName = "swn"
                }
            };
        }

        [SetUp]
        public void Setup()
        {
            _mediator = new Mock<IMediator>();
            _logger = new Mock<ILogger>();
            _emailMock = new Mock<IEmailService>();
            _orderRepoMock = new Mock<IOrderRepository>();
            _mockMediatr = new Mock<IMediator>();

            //Mapper
            var mapperConfig = new MapperConfiguration(
                mc =>
                {
                    mc.AddProfile(new MappingProfile());
                });

            _mapper = mapperConfig.CreateMapper();

            

            //Controller
            _controller = new OrderController(_mockMediatr.Object);
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

