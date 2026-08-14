namespace Order.Api.Domain;

public enum OrderStatus
{
    Draft = 0,
    Pending = 1,
    PaymentReceived = 2,
    Cancelled = 3
}
