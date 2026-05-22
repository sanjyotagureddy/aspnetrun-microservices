using AutoMapper;

using MassTransit;

using MediatR;

using Ordering.Application.Features.Orders.Commands.CheckoutOrder;

using Shared.Messaging.Events;

namespace Ordering.API.EventBusConsumers;

public class BasketCheckoutConsumer(IMapper mapper, IMediator mediator, ILogger<BasketCheckoutConsumer> logger)
  : IConsumer<BasketCheckoutEvent>
{
  private readonly ILogger<BasketCheckoutConsumer> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
  private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

  public async Task Consume(ConsumeContext<BasketCheckoutEvent> context)
  {
    var command = _mapper.Map<CheckoutOrderCommand>(context.Message);
    var result = await _mediator.Send(command);

    _logger.LogInformation("BasketCheckoutEvent consumed successfully. Created Order Id : {newOrderId}", result);
  }
}