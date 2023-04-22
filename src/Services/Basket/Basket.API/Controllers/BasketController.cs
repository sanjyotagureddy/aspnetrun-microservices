using System;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using Basket.API.Entities;
using Basket.API.GrpcServices;
using Basket.API.Repositories.Interfaces;
using EventBus.Messages.Events;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Basket.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class BasketController : ControllerBase
{
  private readonly DiscountGrpcService _discountGrpcService;
  private readonly IMapper _mapper;
  private readonly IPublishEndpoint _publishEndpoint;
  private readonly IBasketRepository _repository;

  public BasketController(IBasketRepository repository, DiscountGrpcService discountGrpcService,
    IPublishEndpoint publishEndpoint, IMapper mapper)
  {
    _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    _discountGrpcService = discountGrpcService ?? throw new ArgumentNullException(nameof(discountGrpcService));
    _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
    _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
  }

  [HttpGet("{userName}", Name = "GetBasket")]
  [ProducesResponseType(typeof(ShoppingCart), (int)HttpStatusCode.OK)]
  public async Task<ActionResult<ShoppingCart>> GetBasket(string userName)
  {
    var basket = await _repository.GetBasket(userName);
    return Ok(basket ?? new ShoppingCart(userName));
  }

  [HttpPost]
  [ProducesResponseType(typeof(ShoppingCart), (int)HttpStatusCode.OK)]
  public async Task<ActionResult<ShoppingCart>> UpdateBasket([FromBody] ShoppingCart basket)
  {
    // TODO: Communicate with Discount.Grpc
    // TODO: Calculate latest prices of Products into the Shopping cart
    // TODO: Consume Discount Grpc

    foreach (var item in basket.Items)
    {
      var coupon = await _discountGrpcService.GetDiscount(item.ProductName);
      item.Price -= coupon.Amount;
    }

    return Ok(await _repository.UpdateBasket(basket));
  }

  [HttpDelete("{userName}", Name = "DeleteBasket")]
  [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
  public async Task<IActionResult> DeleteBasket(string userName)
  {
    await _repository.DeleteBasket(userName);
    return Ok();
  }

  [Route("[action]")]
  [HttpPost]
  [ProducesResponseType((int)HttpStatusCode.Accepted)]
  [ProducesResponseType((int)HttpStatusCode.BadGateway)]
  public async Task<IActionResult> Checkout([FromBody] BasketCheckout basketCheckout)
  {
    // TODO: Get existing basket with total price
    // TODO: Create basketCheckoutEvent --> Set TotalPRice on basketCheckout eventMessage
    // TODO: send checkout event to RabbitMQ
    // TODO: remove the basket

    var basket = await _repository.GetBasket(basketCheckout.UserName);
    if (basket == null)
      return BadRequest();

    var eventMessage = _mapper.Map<BasketCheckoutEvent>(basketCheckout);
    eventMessage.TotalPrice = basket.TotalPrice;

    await _publishEndpoint.Publish(eventMessage);

    await _repository.DeleteBasket(basket.UserName);
    return Accepted();
  }
}