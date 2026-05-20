using AutoMapper;
using MediatR;
using Ordering.Application.Contracts.Persistence;

namespace Ordering.Application.Features.Orders.Queries.GetOrdersList;

public class GetOrdersListQueryHandler(IOrderRepository orderRepository, IMapper mapper)
  : IRequestHandler<GetOrdersListQuery, List<OrdersVm>>
{
  private readonly IMapper _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
  private readonly IOrderRepository _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));

  public async Task<List<OrdersVm>> Handle(GetOrdersListQuery request, CancellationToken cancellationToken)
  {
    var ordersList = await _orderRepository.GetOrdersByUserName(request.UserName);
    return _mapper.Map<List<OrdersVm>>(ordersList);
  }
}