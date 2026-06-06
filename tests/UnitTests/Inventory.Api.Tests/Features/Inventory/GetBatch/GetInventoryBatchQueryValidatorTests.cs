using Inventory.Api.Features.Inventory.GetBatch;

namespace Inventory.Api.Tests.Features.Inventory.GetBatch;

public sealed class GetInventoryBatchQueryValidatorTests
{
    [Fact]
    public async Task Validate_ShouldFail_WhenProductIdsEmpty()
    {
        GetInventoryBatchQueryValidator validator = new();

        var result = await validator.ValidateAsync(new GetInventoryBatchQuery([]), CancellationToken.None);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenProductIdsProvided()
    {
        GetInventoryBatchQueryValidator validator = new();

        var result = await validator.ValidateAsync(new GetInventoryBatchQuery([Guid.NewGuid()]), CancellationToken.None);

        Assert.True(result.IsValid);
    }
}
