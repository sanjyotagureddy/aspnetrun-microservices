using Inventory.Api.Features.Inventory.GetByProductId;

namespace Inventory.Api.Tests.Features.Inventory.GetByProductId;

public sealed class GetInventoryByProductIdQueryValidatorTests
{
    [Fact]
    public async Task Validate_ShouldFail_WhenProductIdIsEmpty()
    {
        GetInventoryByProductIdQueryValidator validator = new();

        var result = await validator.ValidateAsync(new GetInventoryByProductIdQuery(Guid.Empty), CancellationToken.None);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenProductIdIsSet()
    {
        GetInventoryByProductIdQueryValidator validator = new();

        var result = await validator.ValidateAsync(new GetInventoryByProductIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsValid);
    }
}
