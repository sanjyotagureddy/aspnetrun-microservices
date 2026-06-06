using Common.SharedKernel.Exceptions;
using Inventory.Api.Domain;

namespace Inventory.Api.Tests.Domain;

public sealed class InventoryItemTests
{
    [Fact]
    public void Ctor_ShouldInitializeFields_WhenArgumentsAreValid()
    {
        Guid productId = Guid.NewGuid();
        DateTime createdAtUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        InventoryItem item = new(productId, 10, createdAtUtc);

        Assert.Equal(productId, item.ProductId);
        Assert.Equal(10, item.StockQuantity);
        Assert.Equal(createdAtUtc, item.CreatedAtUtc);
        Assert.Equal(createdAtUtc, item.UpdatedAtUtc);
    }

    [Fact]
    public void Ctor_ShouldThrow_WhenProductIdIsEmpty()
    {
        DateTime createdAtUtc = DateTime.UtcNow;

        ValidationException ex = Assert.Throws<ValidationException>(() => new InventoryItem(Guid.Empty, 1, createdAtUtc));

        Assert.Equal("productId", ex.ParamName);
    }

    [Fact]
    public void Ctor_ShouldThrow_WhenStockIsNegative()
    {
        DateTime createdAtUtc = DateTime.UtcNow;

        ValidationException ex = Assert.Throws<ValidationException>(() => new InventoryItem(Guid.NewGuid(), -1, createdAtUtc));

        Assert.Equal("stockQuantity", ex.ParamName);
    }

    [Fact]
    public void Initialize_ShouldUpdateStockAndUpdatedAt()
    {
        DateTime createdAtUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime updatedAtUtc = createdAtUtc.AddMinutes(5);
        InventoryItem item = new(Guid.NewGuid(), 2, createdAtUtc);

        item.Initialize(8, updatedAtUtc);

        Assert.Equal(8, item.StockQuantity);
        Assert.Equal(updatedAtUtc, item.UpdatedAtUtc);
    }
}
