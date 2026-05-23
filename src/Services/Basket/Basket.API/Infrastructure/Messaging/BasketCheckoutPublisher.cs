using Basket.API.Application.Contracts.Infrastructure;
using MassTransit;
using Shared.Messaging.Events;

namespace Basket.API.Infrastructure.Messaging;

internal sealed class BasketCheckoutPublisher(IPublishEndpoint publishEndpoint) : IBasketCheckoutPublisher
{
  private readonly IPublishEndpoint _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));

  public Task PublishAsync(BasketCheckoutEvent eventMessage, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(eventMessage);
    return _publishEndpoint.Publish(eventMessage, cancellationToken);
  }
}