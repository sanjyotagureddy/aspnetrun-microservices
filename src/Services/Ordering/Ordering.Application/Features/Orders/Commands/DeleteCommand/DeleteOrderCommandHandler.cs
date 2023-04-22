using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using Ordering.Application.Contracts.Persistence;
using Ordering.Application.Exceptions;
using Ordering.Domain.Entities;

namespace Ordering.Application.Features.Orders.Commands.DeleteCommand;

public class DeleteOrderCommandHandler : IRequestHandler<DeleteOrderCommand>
{
  private readonly ILogger<DeleteOrderCommandHandler> _logger;
  private readonly IMapper _mapper;
  private readonly IOrderRepository _orderRepository;

  public DeleteOrderCommandHandler(IOrderRepository orderRepository, IMapper mapper,
    ILogger<DeleteOrderCommandHandler> logger)
  {
    _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
    _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  }

  public async Task<Unit> Handle(DeleteOrderCommand request, CancellationToken cancellationToken)
  {
    var orderToDelete = await _orderRepository.GetByIdAsync(request.Id);
    if (orderToDelete == null)
    {
      throw new NotFoundException(nameof(Order), request.Id);
    }

    await _orderRepository.DeleteAsync(orderToDelete);
    _logger.LogInformation($"Order {orderToDelete.Id} is successfully deleted");

    return Unit.Value;
  }
}