namespace Inventory.Api.Features.Inventory.GetBatch;

internal sealed class GetInventoryBatchQueryValidator : AbstractValidator<GetInventoryBatchQuery>
{
    public GetInventoryBatchQueryValidator()
    {
        RuleFor(x => x.ProductIds).NotNull();
        RuleFor(x => x.ProductIds).Must(ids => ids.Count > 0)
            .WithMessage("At least one product id is required.");
    }
}
