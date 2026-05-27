namespace Common.SharedKernel.Logging;

internal sealed class MinimumLevelFilter(LogLevel minimumLevel) : ILogFilter
{
    public bool IsEnabled(LogEntry entry) => entry.Level >= minimumLevel;
}

internal sealed class CategoryPrefixFilter(string categoryPrefix) : ILogFilter
{
    public bool IsEnabled(LogEntry entry)
        => !string.IsNullOrWhiteSpace(categoryPrefix) && entry.Category.StartsWith(categoryPrefix, StringComparison.OrdinalIgnoreCase);
}

internal sealed class PropertyFilter(string propertyName, object? expectedValue) : ILogFilter
{
    public bool IsEnabled(LogEntry entry)
    {
        if (entry.Properties is null || !entry.Properties.TryGetValue(propertyName, out object? value))
        {
            return false;
        }

        return Equals(value, expectedValue);
    }
}
