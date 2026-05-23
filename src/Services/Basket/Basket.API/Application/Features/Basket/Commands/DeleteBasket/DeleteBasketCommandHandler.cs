using Basket.API.Application.Contracts.Persistence;
using MediatR;

namespace Basket.API.Application.Features.Basket.Commands.DeleteBasket;

internal sealed class DeleteBasketCommandHandler(IBasketRepository repository) : IRequestHandler<DeleteBasketCommand, Unit>
{
  private readonly IBasketRepository _repository = repository ?? throw new ArgumentNullException(nameof(repository));

  public async Task<Unit> Handle(DeleteBasketCommand request, CancellationToken cancellationToken)
  {
    ArgumentNullException.ThrowIfNull(request);
    await _repository.DeleteBasketAsync(request.UserName, cancellationToken);
    return Unit.Value;
  }
}