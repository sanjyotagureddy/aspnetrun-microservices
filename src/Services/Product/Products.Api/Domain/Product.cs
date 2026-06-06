using Common.SharedKernel.Abstractions.Auditing;
using Common.SharedKernel.Helpers;

namespace Products.Api.Domain;

internal sealed class Product : AuditableEntity<Guid>
{
    public Product(
        Guid id,
        string name,
        string description,
        string sku,
        decimal price,
        string currency,
        string category,
        string brand,
        bool isActive,
        DateTime createdAtUtc)
        : base(id)
    {
        UpdateCore(name, description, sku, price, currency, category, brand, isActive);
        SetCreatedAudit(createdAtUtc);
    }

    public string Name { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public string Sku { get; private set; } = string.Empty;

    public decimal Price { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    public string Category { get; private set; } = string.Empty;

    public string Brand { get; private set; } = string.Empty;


    public bool IsActive { get; private set; }

    public DateTime CreatedAt => CreatedOnUtc ?? DateTime.MinValue;

    public DateTime UpdatedAt => UpdatedOnUtc ?? CreatedAt;

    public void Update(
        string name,
        string description,
        string sku,
        decimal price,
        string currency,
        string category,
        string brand,
        bool isActive,
        DateTime updatedAtUtc)
    {
        UpdateCore(name, description, sku, price, currency, category, brand, isActive);
        SetUpdatedAudit(updatedAtUtc);
    }

    private void UpdateCore(
        string name,
        string description,
        string sku,
        decimal price,
        string currency,
        string category,
        string brand,
        bool isActive)
    {
        Name = Guard.Against.NullOrWhiteSpace(name).Trim();
        Description = Guard.Against.NullOrWhiteSpace(description).Trim();
        Sku = Guard.Against.NullOrWhiteSpace(sku).Trim().ToUpperInvariant();
        Currency = Guard.Against.NullOrWhiteSpace(currency).Trim().ToUpperInvariant();
        Category = Guard.Against.NullOrWhiteSpace(category).Trim();
        Brand = Guard.Against.NullOrWhiteSpace(brand).Trim();
        Price = price;
        IsActive = isActive;
    }
}