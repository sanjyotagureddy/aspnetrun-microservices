using Cart.Api.Data;
using Cart.Api.Events;
using Common.SharedKernel.Messaging;
using FluentValidation;
using MediatR;

namespace Cart.Api.Features.Cart.Checkout;

public record CartCheckoutDto(
    string UserName,
    string FirstName,
    string LastName,
    string EmailAddress,
    string AddressLine,
    string Country,
    string State,
    string ZipCode,
    string CardName,
    string CardNumber,
    string Expiration,
    string CVV,
    int PaymentMethod);

public record CheckoutCartCommand(CartCheckoutDto CheckoutDto) : IRequest<bool>;

public class CheckoutCartValidator : AbstractValidator<CheckoutCartCommand>
{
    public CheckoutCartValidator()
    {
        RuleFor(x => x.CheckoutDto).NotNull();
        RuleFor(x => x.CheckoutDto.UserName).NotEmpty().WithMessage("UserName is required.");
        RuleFor(x => x.CheckoutDto.EmailAddress).NotEmpty().EmailAddress();
    }
}

public class CheckoutCartHandler : IRequestHandler<CheckoutCartCommand, bool>
{
    private readonly ICartRepository _repository;
    private readonly IMessageBus _messageBus;
    private readonly TimeProvider _timeProvider;

    public CheckoutCartHandler(
        ICartRepository repository,
        IMessageBus messageBus,
        TimeProvider timeProvider)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _messageBus = messageBus ?? throw new ArgumentNullException(nameof(messageBus));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<bool> Handle(CheckoutCartCommand request, CancellationToken cancellationToken)
    {
        var dto = request.CheckoutDto;
        var cart = await _repository.GetCartAsync(dto.UserName, cancellationToken);
        if (cart is null || cart.Items.Count == 0)
        {
            return false;
        }

        var checkoutEvent = new BasketCheckoutIntegrationEvent(
            dto.UserName,
            cart.TotalPrice,
            dto.FirstName,
            dto.LastName,
            dto.EmailAddress,
            dto.AddressLine,
            dto.Country,
            dto.State,
            dto.ZipCode,
            dto.CardName,
            dto.CardNumber,
            dto.Expiration,
            dto.CVV,
            dto.PaymentMethod,
            _timeProvider.GetUtcNow().UtcDateTime);

        await _messageBus.PublishAsync(
            BasketCheckoutIntegrationEvent.Topic,
            checkoutEvent,
            meta => meta.OrderingKey = dto.UserName,
            cancellationToken: cancellationToken);

        await _repository.DeleteCartAsync(dto.UserName, cancellationToken);
        return true;
    }
}
