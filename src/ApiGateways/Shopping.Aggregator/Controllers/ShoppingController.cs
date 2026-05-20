using System.Net;
using Microsoft.AspNetCore.Mvc;
using Shopping.Aggregator.Models;
using Shopping.Aggregator.Services.Interfaces;

namespace Shopping.Aggregator.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class ShoppingController(
  ICatalogService catalogService,
  IBasketService basketService,
  IOrderService orderService)
  : ControllerBase
{
  private readonly IBasketService _basketService = basketService ?? throw new ArgumentNullException(nameof(basketService));
  private readonly ICatalogService _catalogService = catalogService ?? throw new ArgumentNullException(nameof(catalogService));
  private readonly IOrderService _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));

  [HttpGet("{userName}", Name = "GetShopping")]
  [ProducesResponseType(typeof(ShoppingModel), (int)HttpStatusCode.OK)]
  public async Task<ActionResult<ShoppingModel>> GetShopping(string userName)
  {
    // get basket with userName
    // iterate basket items and consume products with basket item productId member
    // map product related members into basketItem dto with extended columns
    // consume ordering micro-services in order to retrieve order list
    // return root ShoppingModel dto class which including all responses

    var basket = await _basketService.GetBasket(userName);
    foreach (var item in basket.Items)
    {
      var product = await _catalogService.GetCatalog(item.ProductId);

      // set additional product fields onto basket item
      item.ProductName = product.Name;
      item.Category = product.Category;
      item.Summary = product.Summary;
      item.Description = product.Description;
      item.ImageFile = product.ImageFile;
    }

    var orders = await _orderService.GetOrdersByUserName(userName);
    var shoppingModel = new ShoppingModel
    {
      UserName = userName,
      BasketWithProducts = basket,
      Orders = orders
    };
    return Ok(shoppingModel);
  }
}