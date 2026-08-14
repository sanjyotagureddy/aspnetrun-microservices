using Common.SharedKernel.Messaging;
using Order.Api.Events;

namespace Inventory.Api.Messaging;

public class OrderCreatedConsumer : BackgroundService
{
    private readonly IMessageConsumer _consumer;
    private readonly OrderCreatedIntegrationEventHandler _handler;

    public OrderCreatedConsumer(
        IMessageConsumer consumer,
        OrderCreatedIntegrationEventHandler handler)
    {
        _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _consumer.SubscribeAsync(
            OrderCreatedIntegrationEvent.Topic,
            _handler,
            stoppingToken);
    }
}
