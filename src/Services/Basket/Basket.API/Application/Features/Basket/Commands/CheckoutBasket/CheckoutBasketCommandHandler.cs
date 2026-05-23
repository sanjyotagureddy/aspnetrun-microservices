using Basket.API.Application.Contracts.Infrastructure;
using Basket.API.Application.Contracts.Persistence;
using MediatR;
using Shared.Messaging.Events;
using SharedKernel.Errors;

namespace Basket.API.Application.Features.Basket.Commands.CheckoutBasket;

internal sealed class CheckoutBasketCommandHandler(IBasketRepository repository, IBasketCheckoutPublisher publisher)
  : IRequestHandler<CheckoutBasketCommand, Unit>
{
  private readonly IBasketCheckoutPublisher _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
  private readonly IBasketRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

  public async Task<Unit> Handle(CheckoutBasketCommand request, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(request);

    var basket = await _repository.GetBasketAsync(request.UserName, cancellationToken);
    if (basket is null)
    {
      throw Errors.ServerSide.NotFound($"Basket for user '{request.UserName}' was not found.");
    }

    var eventMessage = new BasketCheckoutEvent
    {
      UserName = request.UserName,
      TotalPrice = basket.TotalPrice,
      FirstName = request.FirstName,
      LastName = request.LastName,
      EmailAddress = request.EmailAddress,
      AddressLine = request.AddressLine,
      Country = request.Country,
      State = request.State,
      ZipCode = request.ZipCode,
      CardName = request.CardName,
      CardNumber = request.CardNumber,
      Expiration = request.Expiration,
      CVV = request.Cvv,
      PaymentMethod = request.PaymentMethod
    };

    await _publisher.PublishAsync(eventMessage, cancellationToken);
    await _repository.DeleteBasketAsync(request.UserName, cancellationToken);
    return Unit.Value;
  }
}