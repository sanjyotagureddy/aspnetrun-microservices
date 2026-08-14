using Cart.Api.Data;
using MediatR;

namespace Cart.Api.Features.Cart.DeleteCart;

public record DeleteCartCommand(string UserName) : IRequest<bool>;

public class DeleteCartHandler : IRequestHandler<DeleteCartCommand, bool>
{
    private readonly ICartRepository _repository;

    public DeleteCartHandler(ICartRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<bool> Handle(DeleteCartCommand request, CancellationToken cancellationToken)
    {
        return await _repository.DeleteCartAsync(request.UserName, cancellationToken);
    }
}
