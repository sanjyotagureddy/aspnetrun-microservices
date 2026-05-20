using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Ordering.Application.Contracts.Infrastructure;
using Ordering.Application.Contracts.Persistence;
using Ordering.Application.Models;
using Ordering.Domain.Entities;

namespace Ordering.Application.Features.Orders.Commands.CheckoutOrder;

public class CheckoutOrderCommandHandler(
  IOrderRepository orderRepository,
  IMapper mapper,
  IEmailService emailService,
  ILogger<CheckoutOrderCommandHandler> logger)
  : IRequestHandler<CheckoutOrderCommand, int>
{
  private readonly IEmailService _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
  private readonly ILogger<CheckoutOrderCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
  private readonly IOrderRepository _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));

  public async Task<int> Handle(CheckoutOrderCommand request, CancellationToken cancellationToken)
  {
    var orderEntity = _mapper.Map<Order>(request);
    var newOrder = await _orderRepository.AddAsync(orderEntity);
    _logger.LogInformation($"Order {newOrder.Id} is successfully created");

    await SendEmail(newOrder);

    return newOrder.Id;
  }

  private async Task SendEmail(Order newOrder)
  {
    var email = new Email
    {
      To = "sanjyot.agureddy@hotmail.com",
      Body = $"Order {newOrder.Id} is successfully created",
      Subject = $"Order {newOrder.Id} is successfully created"
    };

    try
    {
      await _emailService.SendEmail(email);
    }
    catch (Exception ex)
    {
      _logger.LogError($"Order {newOrder.Id} failed due to an error with the mail service: {ex.Message}");
    }
  }
}