using Cart.Api.Data;
using Cart.Api.Domain;
using Discount.Grpc.Protos;
using FluentValidation;
using MediatR;

namespace Cart.Api.Features.Cart.UpdateCart;

public record UpdateCartCommand(ShoppingCart Cart) : IRequest<ShoppingCart>;

public class UpdateCartValidator : AbstractValidator<UpdateCartCommand>
{
    public UpdateCartValidator()
    {
        RuleFor(x => x.Cart).NotNull();
        RuleFor(x => x.Cart.UserName).NotEmpty().WithMessage("UserName is required.");
    }
}

public class UpdateCartHandler : IRequestHandler<UpdateCartCommand, ShoppingCart>
{
    private readonly ICartRepository _repository;
    private readonly DiscountProtoService.DiscountProtoServiceClient _discountGrpcClient;

    public UpdateCartHandler(
        ICartRepository repository,
        DiscountProtoService.DiscountProtoServiceClient discountGrpcClient)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _discountGrpcClient = discountGrpcClient ?? throw new ArgumentNullException(nameof(discountGrpcClient));
    }

    public async Task<ShoppingCart> Handle(UpdateCartCommand request, CancellationToken cancellationToken)
    {
        foreach (var item in request.Cart.Items)
        {
            try
            {
                var coupon = await _discountGrpcClient.GetDiscountAsync(
                    new GetDiscountRequest { ProductName = item.ProductName },
                    cancellationToken: cancellationToken);

                if (coupon is not null && coupon.Amount > 0)
                {
                    item.Price -= coupon.Amount;
                    if (item.Price < 0) item.Price = 0;
                }
            }
            catch
            {
                // Fallback gracefully if discount service is unavailable or throws
            }
        }

        return await _repository.UpdateCartAsync(request.Cart, cancellationToken);
    }
}
