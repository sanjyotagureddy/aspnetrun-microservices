using Moq;
using Microsoft.AspNetCore.Http.HttpResults;
using Shopping.Aggregator.Endpoints;
using Shopping.Aggregator.Models;
using Shopping.Aggregator.Services.Interfaces;
using Xunit;

namespace Shopping.Aggregator.Test;

public class ShoppingEndpointTest
{
  private readonly Mock<IBasketService> _basketService = new();
  private readonly Mock<ICatalogService> _catalogService = new();
  private readonly Mock<IOrderService> _orderService = new();

  private BasketModel _basketModel;
  private CatalogModel _catalogModel;
  private IEnumerable<OrderResponseModel> _orderResponseModel;

  public ShoppingEndpointTest()
  {
    _basketService.Reset();
    _catalogService.Reset();
    _orderService.Reset();
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
      Name = "Phone",
      Category = "abc",
      Summary = "Summary",
      Description = "Description",
      ImageFile = "image.png",
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

  [Theory]
  [InlineData("swn")]
  public async Task GetShopping_Returns_ShoppingModel_WithEnrichedBasketAsync(string userName)
  {
    _basketService.Setup(c => c.GetBasket(userName)).ReturnsAsync(_basketModel);
    _catalogService.Setup(p => p.GetCatalog(It.IsAny<string>())).ReturnsAsync(_catalogModel);
    _orderService.Setup(p => p.GetOrdersByUserName(userName)).ReturnsAsync(_orderResponseModel);

    Ok<ShoppingModel> result = await ShoppingEndpoints.GetShopping(
      _catalogService.Object,
      _basketService.Object,
      _orderService.Object,
      userName);

    Assert.NotNull(result);

    var shoppingModel = result.Value;
    Assert.NotNull(shoppingModel);
    Assert.Equal(userName, shoppingModel!.UserName);
    Assert.Same(_basketModel, shoppingModel.BasketWithProducts);
    Assert.Same(_orderResponseModel, shoppingModel.Orders);

    var item = shoppingModel.BasketWithProducts.Items.Single();
    Assert.Equal(_catalogModel.Name, item.ProductName);
    Assert.Equal(_catalogModel.Category, item.Category);
    Assert.Equal(_catalogModel.Summary, item.Summary);
    Assert.Equal(_catalogModel.Description, item.Description);
    Assert.Equal(_catalogModel.ImageFile, item.ImageFile);
    _basketService.Verify(c => c.GetBasket(userName), Times.Once);
    _catalogService.Verify(p => p.GetCatalog(_basketModel.Items[0].ProductId), Times.Once);
    _orderService.Verify(p => p.GetOrdersByUserName(userName), Times.Once);
  }

  [Fact]
  public async Task GetShopping_Enriches_EachBasketItemAsync()
  {
    _basketModel.Items.Add(new BasketItemExtendedModel
    {
      ProductId = "98765432100000",
      Category = "def",
      Quantity = 2,
      Price = 99
    });

    _basketService.Setup(c => c.GetBasket("swn")).ReturnsAsync(_basketModel);
    _catalogService.Setup(p => p.GetCatalog("12345678987456")).ReturnsAsync(_catalogModel);
    _catalogService.Setup(p => p.GetCatalog("98765432100000")).ReturnsAsync(new CatalogModel
    {
      Id = "98765432100000",
      Name = "Tablet",
      Category = "def",
      Summary = "Summary 2",
      Description = "Description 2",
      ImageFile = "tablet.png",
      Price = 99
    });
    _orderService.Setup(p => p.GetOrdersByUserName("swn")).ReturnsAsync(_orderResponseModel);

    Ok<ShoppingModel> result = await ShoppingEndpoints.GetShopping(
      _catalogService.Object,
      _basketService.Object,
      _orderService.Object,
      "swn");

    var shoppingModel = result.Value;
    Assert.NotNull(shoppingModel);
    Assert.Equal(2, shoppingModel!.BasketWithProducts.Items.Count);
    Assert.Equal(_catalogModel.Name, shoppingModel.BasketWithProducts.Items[0].ProductName);
    Assert.Equal("Tablet", shoppingModel.BasketWithProducts.Items[1].ProductName);
    _catalogService.Verify(p => p.GetCatalog(It.IsAny<string>()), Times.Exactly(2));
  }
}