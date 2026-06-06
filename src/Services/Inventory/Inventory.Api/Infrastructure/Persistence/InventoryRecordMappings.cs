namespace Inventory.Api.Infrastructure.Persistence;

internal sealed class InventoryItemRecord
{
    public Guid ProductId { get; set; }

    public int StockQuantity { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public InventoryItem ToDomain()
    {
        InventoryItem item = new(ProductId, StockQuantity, CreatedAt);
        item.Initialize(StockQuantity, UpdatedAt);
        return item;
    }
}

internal static class InventoryRecordMappings
{
    public static object ToRecord(this InventoryItem item)
    {
        return new
        {
            item.ProductId,
            item.StockQuantity,
            CreatedAt = item.CreatedAtUtc,
            UpdatedAt = item.UpdatedAtUtc
        };
    }

    public static InventoryResponse ToResponse(this InventoryItem item)
    {
        return new InventoryResponse(item.ProductId, item.StockQuantity);
    }
}
