using Cart.Api.Data;
using Cart.Api.Domain;
using MediatR;

namespace Cart.Api.Features.Cart.GetCart;

public record GetCartQuery(string UserName) : IRequest<ShoppingCart>;

public class GetCartHandler : IRequestHandler<GetCartQuery, ShoppingCart>
{
    private readonly ICartRepository _repository;

    public GetCartHandler(ICartRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<ShoppingCart> Handle(GetCartQuery request, CancellationToken cancellationToken)
    {
        return await _repository.GetCartAsync(request.UserName, cancellationToken);
    }
}
