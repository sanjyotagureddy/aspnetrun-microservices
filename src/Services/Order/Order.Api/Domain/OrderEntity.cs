namespace Order.Api.Domain;

public class OrderEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserName { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string CardName { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public string Expiration { get; set; } = string.Empty;
    public string CVV { get; set; } = string.Empty;
    public int PaymentMethod { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public List<OrderItem> Items { get; set; } = new();
}
