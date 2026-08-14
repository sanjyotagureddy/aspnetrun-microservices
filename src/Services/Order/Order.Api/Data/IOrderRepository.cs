using Order.Api.Domain;

namespace Order.Api.Data;

public interface IOrderRepository
{
    Task<OrderEntity> CreateOrderAsync(OrderEntity order, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderEntity>> GetOrdersByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task<OrderEntity?> GetOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> CancelOrderAsync(Guid id, CancellationToken cancellationToken = default);
}
