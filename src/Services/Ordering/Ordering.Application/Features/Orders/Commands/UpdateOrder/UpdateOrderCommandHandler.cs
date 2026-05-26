using AutoMapper;

using MediatR;

using Microsoft.Extensions.Logging;

using Ordering.Application.Contracts.Persistence;
using Ordering.Application.Exceptions;
using Ordering.Domain.Entities;

namespace Ordering.Application.Features.Orders.Commands.UpdateOrder;

public class UpdateOrderCommandHandler(IOrderRepository orderRepository, IMapper mapper, ILogger<UpdateOrderCommandHandler> logger)
  : IRequestHandler<UpdateOrderCommand>
{
    private readonly ILogger<UpdateOrderCommandHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    private readonly IOrderRepository _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));

    public async Task Handle(UpdateOrderCommand request, CancellationToken cancellationToken)
    {
      var orderToUpdate = await _orderRepository.GetByIdAsync(request.Id, cancellationToken);
        if (orderToUpdate == null) throw new NotFoundException(nameof(Order), request.Id);

        _mapper.Map(request, orderToUpdate, typeof(UpdateOrderCommand), typeof(Order));
      await _orderRepository.UpdateAsync(orderToUpdate, cancellationToken);
        _logger.LogInformation("Order {OrderId} was updated successfully", orderToUpdate.Id);
    }
}