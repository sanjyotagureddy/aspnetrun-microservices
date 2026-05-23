using MediatR;

namespace Basket.API.Application.Features.Basket.Commands.CheckoutBasket;

public sealed record CheckoutBasketCommand : IRequest<Unit>
{
  public string UserName { get; init; } = string.Empty;

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

  public string Cvv { get; init; } = string.Empty;

  public int PaymentMethod { get; init; }
}