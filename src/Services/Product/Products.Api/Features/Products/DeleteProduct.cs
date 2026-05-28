using Common.SharedKernel.Exceptions;

namespace Products.Api.Features.Products;

internal sealed class DeleteProductEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapProductsV1();

        group.MapDelete("/{id:guid}", HandleAsync)
            .WithName(ProductRouteNames.Delete);
    }

    private static async Task<IResult> HandleAsync(IMediator mediator, Guid id, CancellationToken cancellationToken)
    {
        await mediator.Send(new DeleteProductCommand(id), cancellationToken);
        return TypedResults.NoContent();
    }
}

internal sealed record DeleteProductCommand(Guid Id) : IRequest<Result>;

internal sealed class DeleteProductCommandValidator : AbstractValidator<DeleteProductCommand>
{
    public DeleteProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}

internal sealed class DeleteProductCommandHandler(IProductCatalogStore store)
    : IRequestHandler<DeleteProductCommand, Result>
{
    public async Task<Result> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var deleted = await store.DeleteAsync(request.Id, cancellationToken);
        return !deleted ? throw new NotFoundException(nameof(Product), request.Id.ToString()) : Result.Success();
    }
}
