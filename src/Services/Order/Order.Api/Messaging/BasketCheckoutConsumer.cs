using Cart.Api.Events;
using Common.SharedKernel.Messaging;

namespace Order.Api.Messaging;

public class BasketCheckoutConsumer : BackgroundService
{
    private readonly IMessageConsumer _consumer;
    private readonly BasketCheckoutIntegrationEventHandler _handler;

    public BasketCheckoutConsumer(
        IMessageConsumer consumer,
        BasketCheckoutIntegrationEventHandler handler)
    {
        _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _consumer.SubscribeAsync(
            BasketCheckoutIntegrationEvent.Topic,
            _handler,
            stoppingToken);
    }
}
