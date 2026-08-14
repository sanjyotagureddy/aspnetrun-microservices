using MediatR;
using Order.Api.Data;

namespace Order.Api.Features.Orders.CancelOrder;

public record CancelOrderCommand(Guid Id) : IRequest<bool>;

public class CancelOrderHandler : IRequestHandler<CancelOrderCommand, bool>
{
    private readonly IOrderRepository _repository;

    public CancelOrderHandler(IOrderRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<bool> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        return await _repository.CancelOrderAsync(request.Id, cancellationToken);
    }
}
