using System.Net;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using Shopping.Aggregator.Controllers;
using Shopping.Aggregator.Models;
using Shopping.Aggregator.Services.Interfaces;

namespace Shopping.Aggregator.Test;

public class ShoppingControllerTest
{
  private readonly Mock<IBasketService> _basketService = new();
  private readonly Mock<ICatalogService> _catalogService = new();
  private readonly Mock<IOrderService> _orderService = new();

  private BasketModel _basketModel;
  private CatalogModel _catalogModel;

  private ShoppingController _controller;
  private IEnumerable<OrderResponseModel> _orderResponseModel;

  [SetUp]
  public void Setup()
  {
    _controller = new ShoppingController(_catalogService.Object, _basketService.Object, _orderService.Object);
    _basketModel = new BasketModel
    {
      UserName = "swn",
      TotalPrice = 1100,
      Items = new List<BasketItemExtendedModel>
      {
        new()
        {
          ProductId = "12345678987456",
          Category = "abc",
          Quantity = 1,
          Price = 350
        }
      }
    };

    _catalogModel = new CatalogModel
    {
      Id = "12345678987654",
      Category = "abc",
      Price = 1100
    };

    _orderResponseModel = new List<OrderResponseModel>
    {
      new()
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
    _orderService.Setup(p => p.GetOrdersByUserName(userName)).ReturnsAsync(_orderResponseModel);

    var result = await _controller.GetShopping(userName);
    if (result.Result is OkObjectResult okResult)
      Assert.AreEqual((int)HttpStatusCode.OK, okResult.StatusCode);
    else
      Assert.Fail();
  }
}