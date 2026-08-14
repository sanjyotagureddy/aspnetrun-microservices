using MediatR;
using Order.Api.Data;
using Order.Api.Domain;

namespace Order.Api.Features.Orders.GetOrderById;

public record GetOrderByIdQuery(Guid Id) : IRequest<OrderEntity?>;

public class GetOrderByIdHandler : IRequestHandler<GetOrderByIdQuery, OrderEntity?>
{
    private readonly IOrderRepository _repository;

    public GetOrderByIdHandler(IOrderRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<OrderEntity?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetOrderByIdAsync(request.Id, cancellationToken);
    }
}
