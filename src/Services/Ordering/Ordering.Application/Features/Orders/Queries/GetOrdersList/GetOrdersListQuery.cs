using MediatR;

namespace Ordering.Application.Features.Orders.Queries.GetOrdersList;

public class GetOrdersListQuery(string userName) : IRequest<List<OrdersVm>>
{
  public string UserName { get; set; } = userName ?? throw new ArgumentNullException(nameof(userName));
}