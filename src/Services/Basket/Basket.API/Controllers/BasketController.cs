using System.Net;

using Basket.API.Application.Features.Basket.Commands.CheckoutBasket;
using Basket.API.Application.Features.Basket.Commands.DeleteBasket;
using Basket.API.Application.Features.Basket.Commands.UpdateBasket;
using Basket.API.Application.Features.Basket.Queries.GetBasket;
using Basket.API.Domain.Entities;

using MediatR;

using Microsoft.AspNetCore.Mvc;

namespace Basket.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class BasketController(IMediator mediator) : ControllerBase
{
  private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

  [HttpGet("{userName}", Name = "GetBasket")]
  [ProducesResponseType(typeof(ShoppingCart), (int)HttpStatusCode.OK)]
  public async Task<ActionResult<ShoppingCart>> GetBasket(string userName, CancellationToken cancellationToken)
  {
    ShoppingCart basket = await _mediator.Send(new GetBasketQuery(userName), cancellationToken);
    return Ok(basket ?? new ShoppingCart(userName));
  }

  [HttpPost]
  [ProducesResponseType(typeof(ShoppingCart), (int)HttpStatusCode.OK)]
  public async Task<ActionResult<ShoppingCart>> UpdateBasket([FromBody] ShoppingCart basket, CancellationToken cancellationToken)
  {
    return Ok(await _mediator.Send(new UpdateBasketCommand(basket), cancellationToken));
  }

  [HttpDelete("{userName}", Name = "DeleteBasket")]
  [ProducesResponseType(typeof(void), (int)HttpStatusCode.OK)]
  public async Task<IActionResult> DeleteBasket(string userName, CancellationToken cancellationToken)
  {
    await _mediator.Send(new DeleteBasketCommand(userName), cancellationToken);
    return Ok();
  }

  [Route("[action]")]
  [HttpPost]
  [ProducesResponseType((int)HttpStatusCode.Accepted)]
  [ProducesResponseType((int)HttpStatusCode.BadGateway)]
  public async Task<IActionResult> Checkout([FromBody] CheckoutBasketCommand command, CancellationToken cancellationToken)
  {
    await _mediator.Send(command, cancellationToken);
    return Accepted();
  }
}