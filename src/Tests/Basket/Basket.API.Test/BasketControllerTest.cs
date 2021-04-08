using System.Net;
using Basket.API.Controllers;
using Basket.API.Entities;
using Basket.API.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;

namespace Basket.API.Test
{
    public class BasketControllerTest
    {
        private Mock<IBasketRepository> _repository;
        private BasketController _controller;
        private ShoppingCart _shoppingCart;

        [SetUp]
        public void Setup()
        {
            _repository = new Mock<IBasketRepository>();
            _controller = new BasketController(_repository.Object);

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