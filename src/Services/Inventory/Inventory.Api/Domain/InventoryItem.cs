using Common.SharedKernel.Abstractions.Auditing;
using CommonValidationException = Common.SharedKernel.Exceptions.ValidationException;

namespace Inventory.Api.Domain;

internal sealed class InventoryItem : AuditableEntity<Guid>
{
    public InventoryItem(Guid productId, int stockQuantity, DateTime createdAtUtc)
        : base(EnsureProductId(productId))
    {
        SetStockQuantity(stockQuantity);
        SetCreatedAudit(createdAtUtc);
    }

    public Guid ProductId => Id;

    public int StockQuantity { get; private set; }

    public DateTime CreatedAtUtc => CreatedOnUtc ?? DateTime.MinValue;

    public DateTime UpdatedAtUtc => UpdatedOnUtc ?? CreatedAtUtc;

    public void Initialize(int stockQuantity, DateTime updatedAtUtc)
    {
        SetStockQuantity(stockQuantity);
        SetUpdatedAudit(updatedAtUtc);
    }

    private static Guid EnsureProductId(Guid productId)
    {
        return productId == Guid.Empty
            ? throw new CommonValidationException(nameof(productId), "Product id cannot be empty.")
            : productId;
    }

    private void SetStockQuantity(int stockQuantity)
    {
        if (stockQuantity < 0)
        {
            throw new CommonValidationException(nameof(stockQuantity), "Stock quantity must be greater than or equal to 0.");
        }

        StockQuantity = stockQuantity;
    }
}
