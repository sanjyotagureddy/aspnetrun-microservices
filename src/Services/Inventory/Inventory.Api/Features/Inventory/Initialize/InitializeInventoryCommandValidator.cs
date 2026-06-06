namespace Inventory.Api.Features.Inventory.Initialize;

internal sealed class InitializeInventoryCommandValidator : AbstractValidator<InitializeInventoryCommand>
{
    public InitializeInventoryCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty();
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
    }
}
