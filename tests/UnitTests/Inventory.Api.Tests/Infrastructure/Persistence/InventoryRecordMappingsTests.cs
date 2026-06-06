using Inventory.Api.Contracts;
using Inventory.Api.Domain;
using Inventory.Api.Infrastructure.Persistence;

namespace Inventory.Api.Tests.Infrastructure.Persistence;

public sealed class InventoryRecordMappingsTests
{
    [Fact]
    public void ToRecord_ShouldMapInventoryItem()
    {
        DateTime createdAtUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        InventoryItem item = new(Guid.NewGuid(), 7, createdAtUtc);

        object record = item.ToRecord();

        Assert.NotNull(record);
    }

    [Fact]
    public void ToResponse_ShouldMapInventoryItem()
    {
        InventoryItem item = new(Guid.NewGuid(), 9, DateTime.UtcNow);

        InventoryResponse response = item.ToResponse();

        Assert.Equal(item.ProductId, response.ProductId);
        Assert.Equal(9, response.StockQuantity);
    }

    [Fact]
    public void ItemRecord_ToDomain_ShouldMapAndInitialize()
    {
        DateTime createdAtUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime updatedAtUtc = createdAtUtc.AddMinutes(1);
        InventoryItemRecord record = new()
        {
            ProductId = Guid.NewGuid(),
            StockQuantity = 5,
            CreatedAt = createdAtUtc,
            UpdatedAt = updatedAtUtc
        };

        InventoryItem domain = record.ToDomain();

        Assert.Equal(record.ProductId, domain.ProductId);
        Assert.Equal(5, domain.StockQuantity);
        Assert.Equal(updatedAtUtc, domain.UpdatedAtUtc);
    }
}
