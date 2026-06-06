using Inventory.Api.Features.Inventory.Initialize;

namespace Inventory.Api.Tests.Features.Inventory.Initialize;

public sealed class InitializeInventoryCommandValidatorTests
{
    [Fact]
    public async Task Validate_ShouldFail_WhenProductIdEmpty()
    {
        InitializeInventoryCommandValidator validator = new();

        var result = await validator.ValidateAsync(new InitializeInventoryCommand(Guid.Empty, 0), CancellationToken.None);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_ShouldFail_WhenStockNegative()
    {
        InitializeInventoryCommandValidator validator = new();

        var result = await validator.ValidateAsync(new InitializeInventoryCommand(Guid.NewGuid(), -1), CancellationToken.None);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Validate_ShouldPass_WhenValuesValid()
    {
        InitializeInventoryCommandValidator validator = new();

        var result = await validator.ValidateAsync(new InitializeInventoryCommand(Guid.NewGuid(), 0), CancellationToken.None);

        Assert.True(result.IsValid);
    }
}
