using Shared.Messaging.Events;

namespace Basket.API.Application.Contracts.Infrastructure;

public interface IBasketCheckoutPublisher
{
  Task PublishAsync(BasketCheckoutEvent eventMessage, CancellationToken cancellationToken = default);
}