using System.Net;
using Basket.API.Controllers;
using Basket.API.Entities;
using Basket.API.GrpcServices;
using Basket.API.Repositories.Interfaces;
using Discount.Grpc.Protos;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Basket.API.Test
{
    public class BasketControllerTest
    {
        private Mock<IBasketRepository> _repository;
        private Mock<DiscountGrpcService> _grpcService;
        private Mock<DiscountProtoService.DiscountProtoServiceClient> _mock;
        private BasketController _controller;
        private ShoppingCart _shoppingCart;

        [SetUp]
        public void Setup()
        {
            _mock = new Mock<DiscountProtoService.DiscountProtoServiceClient>();
            _repository = new Mock<IBasketRepository>();
            _grpcService = new Mock<DiscountGrpcService>(_mock.Object);

            _controller = new BasketController(_repository.Object, _grpcService.Object);

            _shoppingCart = new ShoppingCart()
            {
                UserName = "swn"
            };
        }

        [TestCase("swn")]
        [Test]
        public void GetBasket(string userName)
        {
            _repository.Setup(p => p.GetBasket(userName)).ReturnsAsync(_shoppingCart);
            var basket = _controller.GetBasket(userName);
            if (basket.Result.Result is OkObjectResult okResult)
                Assert.AreEqual((int)HttpStatusCode.OK, okResult.StatusCode);
            else
                Assert.Fail();
        }

        [TestCase("abc")]
        [TestCase("xyz")]
        [Test]
        public void GetBasket_NotFound(string userName)
        {
            _repository.Setup(p => p.GetBasket(userName)).ReturnsAsync(new ShoppingCart(userName));
            var basket = _controller.GetBasket(userName);
            if (basket.Result.Result is OkObjectResult okResult)
                Assert.AreEqual((int)HttpStatusCode.OK, okResult.StatusCode);
            else
                Assert.Fail();
        }
    }
}