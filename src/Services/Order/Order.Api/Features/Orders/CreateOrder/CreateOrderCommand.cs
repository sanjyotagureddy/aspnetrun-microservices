using FluentValidation;
using MediatR;
using Order.Api.Data;
using Order.Api.Domain;

namespace Order.Api.Features.Orders.CreateOrder;

public record OrderItemDto(string ProductId, string ProductName, decimal UnitPrice, int Quantity);

public record CreateOrderCommand(
    string UserName,
    decimal TotalPrice,
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
    int PaymentMethod,
    List<OrderItemDto> Items) : IRequest<OrderEntity>;

public class CreateOrderValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderValidator()
    {
        RuleFor(x => x.UserName).NotEmpty().WithMessage("UserName is required.");
        RuleFor(x => x.EmailAddress).NotEmpty().EmailAddress();
        RuleFor(x => x.TotalPrice).GreaterThanOrEqualTo(0);
    }
}

public class CreateOrderHandler : IRequestHandler<CreateOrderCommand, OrderEntity>
{
    private readonly IOrderRepository _repository;

    public CreateOrderHandler(IOrderRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<OrderEntity> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var order = new OrderEntity
        {
            UserName = request.UserName,
            TotalPrice = request.TotalPrice,
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
            CVV = request.CVV,
            PaymentMethod = request.PaymentMethod,
            Status = OrderStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            Items = request.Items.Select(i => new OrderItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity
            }).ToList()
        };

        return await _repository.CreateOrderAsync(order, cancellationToken);
    }
}
