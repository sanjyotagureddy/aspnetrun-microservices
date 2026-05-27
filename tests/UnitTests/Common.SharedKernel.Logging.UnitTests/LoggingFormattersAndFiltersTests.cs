using System.Text.Json;

namespace Common.SharedKernel.Logging.UnitTests;

public sealed class LoggingFormattersAndFiltersTests
{
    [Fact]
    public void JsonFormatter_ShouldEmitStructuredJson()
    {
        var entry = LogEntry.Create(
            LogLevel.Information,
            "Catalog",
            "Catalog",
            "Products",
            "Created product",
            DateTimeOffset.Parse("2026-01-01T10:00:00Z"),
            "corr-123",
            null,
            new Dictionary<string, object?> { ["sku"] = "ABC-123" });

        var payload = new JsonLogFormatter().Format(entry);

        using var document = JsonDocument.Parse(payload);

        document.RootElement.GetProperty("serviceName").GetString().Should().Be("Catalog");
        document.RootElement.GetProperty("category").GetString().Should().Be("Products");
        document.RootElement.GetProperty("message").GetString().Should().Be("Created product");
        document.RootElement.GetProperty("correlationId").GetString().Should().Be("corr-123");
        document.RootElement.GetProperty("properties").GetProperty("sku").GetString().Should().Be("ABC-123");
    }

    [Fact]
    public void CategoryPrefixFilter_ShouldMatchExpectedPrefix()
    {
        ILogFilter filter = new CategoryPrefixFilter("Catalog.");
        var entry = LogEntry.Create(LogLevel.Information, "Catalog","Catalog", "Catalog.Products", "Message", DateTimeOffset.UtcNow);

        filter.IsEnabled(entry).Should().BeTrue();
    }

    [Fact]
    public void PropertyFilter_ShouldMatchPropertyValue()
    {
        ILogFilter filter = new PropertyFilter("sku", "ABC-123");
        var entry = LogEntry.Create(
            LogLevel.Information,
            "Catalog",
            "Catalog",
            "Products",
            "Message",
            DateTimeOffset.UtcNow,
            properties: new Dictionary<string, object?> { ["sku"] = "ABC-123" });

        filter.IsEnabled(entry).Should().BeTrue();
    }
}
