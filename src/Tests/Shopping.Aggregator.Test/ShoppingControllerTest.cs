using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using Shopping.Aggregator.Controllers;
using Shopping.Aggregator.Models;
using Shopping.Aggregator.Services.Interfaces;

namespace Shopping.Aggregator.Test
{
    public class ShoppingControllerTest
    {
        private readonly Mock<ICatalogService> _catalogService;
        private readonly Mock<IBasketService> _basketService;
        private readonly Mock<IOrderService> _orderService;

        private ShoppingController _controller;

        private BasketModel _basketModel;
        private CatalogModel _catalogModel;
        private IEnumerable<OrderResponseModel> _orderResponseModel;

        public ShoppingControllerTest()
        {
            _catalogService = new Mock<ICatalogService>();
            _basketService = new Mock<IBasketService>();
            _orderService = new Mock<IOrderService>();
        }

        [SetUp]
        public void Setup()
        {
            _controller = new ShoppingController(_catalogService.Object, _basketService.Object, _orderService.Object);
            _basketModel = new BasketModel()
            {
                UserName = "swn",
                TotalPrice = 1100,
                Items = new List<BasketItemExtendedModel>()
                {
                    new BasketItemExtendedModel()
                    {
                        ProductId = "12345678987456",
                        Category = "abc",
                        Quantity = 1,
                        Price = 350
                    }
                }
            };

            _catalogModel = new CatalogModel()
            {
                Id = "12345678987654",
                Category = "abc",
                Price = 1100
            };

            _orderResponseModel = new List<OrderResponseModel>()
            {
                new OrderResponseModel()
                {
                    UserName = "swn",
                    TotalPrice = 1100
                }
            };
        }

        [Test]
        [TestCase("swn")]
        public async Task GetShopping_Return_OkAsync(string userName)
        {
            _basketService.Setup(c => c.GetBasket(userName)).ReturnsAsync(_basketModel);
            _catalogService.Setup(p => p.GetCatalog(It.IsAny<string>())).ReturnsAsync(_catalogModel);
            _orderService.Setup((p => p.GetOrdersByUserName(userName))).ReturnsAsync(_orderResponseModel);

            var result = await _controller.GetShopping(userName);
            if (result.Result is OkObjectResult okResult)
                Assert.AreEqual((int)HttpStatusCode.OK, okResult.StatusCode);
            else
                Assert.Fail();
        }
    }
}