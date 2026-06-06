namespace Inventory.Api.Features.Inventory.GetByProductId;

internal sealed class GetInventoryByProductIdQueryValidator : AbstractValidator<GetInventoryByProductIdQuery>
{
    public GetInventoryByProductIdQueryValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
    }
}
