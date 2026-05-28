using Common.SharedKernel.Logging;

namespace Common.SharedKernel.Tests.Logging;

public sealed class LogEntryTests
{
    [Fact]
    public void Create_ShouldNormalizeOptionalCorrelationId()
    {
        var entry = LogEntry.Create("test-service", "orders", "created", DateTimeOffset.UtcNow, "   ");

        Assert.Null(entry.CorrelationId);
    }

    [Fact]
    public void Create_ShouldKeepCategoryAndMessage()
    {
        var entry = LogEntry.Create("test-service", "orders", "created", DateTimeOffset.UtcNow);

        Assert.Equal("orders", entry.Category);
        Assert.Equal("created", entry.Message);
    }
}