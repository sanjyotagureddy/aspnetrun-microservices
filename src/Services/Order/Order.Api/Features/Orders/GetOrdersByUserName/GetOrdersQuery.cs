using MediatR;
using Order.Api.Data;
using Order.Api.Domain;

namespace Order.Api.Features.Orders.GetOrdersByUserName;

public record GetOrdersQuery(string UserName) : IRequest<IReadOnlyList<OrderEntity>>;

public class GetOrdersHandler : IRequestHandler<GetOrdersQuery, IReadOnlyList<OrderEntity>>
{
    private readonly IOrderRepository _repository;

    public GetOrdersHandler(IOrderRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<IReadOnlyList<OrderEntity>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetOrdersByUserNameAsync(request.UserName, cancellationToken);
    }
}
