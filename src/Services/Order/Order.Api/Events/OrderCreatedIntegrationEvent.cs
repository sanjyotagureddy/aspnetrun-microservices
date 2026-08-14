using System.Text.Json.Serialization;
using Common.SharedKernel.Abstractions.IntegrationEvents;

namespace Order.Api.Events;

public record OrderItemIntegrationDto(string ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record OrderCreatedIntegrationEvent : IntegrationEventBase
{
    public const string Topic = "orders.events.v1";
    public const string EventTypeName = "order.created";

    public override string EventType => EventTypeName;

    [JsonConstructor]
    public OrderCreatedIntegrationEvent(
        Guid eventId,
        DateTime occurredOnUtc,
        Guid orderId,
        string userName,
        decimal totalPrice,
        List<OrderItemIntegrationDto> items)
        : base(eventId, occurredOnUtc)
    {
        OrderId = orderId;
        UserName = userName;
        TotalPrice = totalPrice;
        Items = items;
    }

    public OrderCreatedIntegrationEvent(
        Guid orderId,
        string userName,
        decimal totalPrice,
        List<OrderItemIntegrationDto> items,
        DateTime occurredOnUtc)
        : this(Guid.NewGuid(), occurredOnUtc, orderId, userName, totalPrice, items)
    {
    }

    public Guid OrderId { get; init; }
    public string UserName { get; init; } = string.Empty;
    public decimal TotalPrice { get; init; }
    public List<OrderItemIntegrationDto> Items { get; init; } = new();
}
