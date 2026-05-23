using Microsoft.AspNetCore.Http.HttpResults;

using SharedKernel.Web;

using Shopping.Aggregator.Models;
using Shopping.Aggregator.Services.Interfaces;

namespace Shopping.Aggregator.Endpoints;

internal sealed class ShoppingEndpoints : IEndpoint
{
  public void MapEndpoints(IEndpointRouteBuilder app)
  {
    RouteGroupBuilder shopping = app.MapGroup("api/v1/shopping")
      .WithTags("Shopping");

    shopping.MapGet("{userName}", GetShopping)
      .WithName("GetShopping");
  }

  internal static async Task<Ok<ShoppingModel>> GetShopping(
    ICatalogService catalogService,
    IBasketService basketService,
    IOrderService orderService,
    string userName)
  {
    var basket = await basketService.GetBasket(userName);
    foreach (var item in basket.Items)
    {
      var product = await catalogService.GetCatalog(item.ProductId);

      item.ProductName = product.Name;
      item.Category = product.Category;
      item.Summary = product.Summary;
      item.Description = product.Description;
      item.ImageFile = product.ImageFile;
    }

    var orders = await orderService.GetOrdersByUserName(userName);
    var shoppingModel = new ShoppingModel
    {
      UserName = userName,
      BasketWithProducts = basket,
      Orders = orders
    };

    return TypedResults.Ok(shoppingModel);
  }
}