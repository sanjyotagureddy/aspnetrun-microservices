using System.Text.Json.Serialization;
using Common.SharedKernel.Abstractions.IntegrationEvents;

namespace Cart.Api.Events;

public record BasketCheckoutIntegrationEvent : IntegrationEventBase
{
    public const string Topic = "cart.events.v1";
    public const string EventTypeName = "cart.checkout";

    public override string EventType => EventTypeName;

    [JsonConstructor]
    public BasketCheckoutIntegrationEvent(
        Guid eventId,
        DateTime occurredOnUtc,
        string userName,
        decimal totalPrice,
        string firstName,
        string lastName,
        string emailAddress,
        string addressLine,
        string country,
        string state,
        string zipCode,
        string cardName,
        string cardNumber,
        string expiration,
        string cvv,
        int paymentMethod)
        : base(eventId, occurredOnUtc)
    {
        UserName = userName;
        TotalPrice = totalPrice;
        FirstName = firstName;
        LastName = lastName;
        EmailAddress = emailAddress;
        AddressLine = addressLine;
        Country = country;
        State = state;
        ZipCode = zipCode;
        CardName = cardName;
        CardNumber = cardNumber;
        Expiration = expiration;
        CVV = cvv;
        PaymentMethod = paymentMethod;
    }

    public BasketCheckoutIntegrationEvent(
        string userName,
        decimal totalPrice,
        string firstName,
        string lastName,
        string emailAddress,
        string addressLine,
        string country,
        string state,
        string zipCode,
        string cardName,
        string cardNumber,
        string expiration,
        string cvv,
        int paymentMethod,
        DateTime occurredOnUtc)
        : this(
            Guid.NewGuid(),
            occurredOnUtc,
            userName,
            totalPrice,
            firstName,
            lastName,
            emailAddress,
            addressLine,
            country,
            state,
            zipCode,
            cardName,
            cardNumber,
            expiration,
            cvv,
            paymentMethod)
    {
    }

    public string UserName { get; init; } = string.Empty;
    public decimal TotalPrice { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string EmailAddress { get; init; } = string.Empty;
    public string AddressLine { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string ZipCode { get; init; } = string.Empty;
    public string CardName { get; init; } = string.Empty;
    public string CardNumber { get; init; } = string.Empty;
    public string Expiration { get; init; } = string.Empty;
    public string CVV { get; init; } = string.Empty;
    public int PaymentMethod { get; init; }
}
