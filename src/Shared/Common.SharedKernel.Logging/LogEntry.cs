namespace Common.SharedKernel.Logging;

public sealed record LogEntry
{
    public LogLevel Level { get; }

    public string ServiceName { get; }
    public string Category { get; }
    public string Namespace { get; }

    public string Message { get; }

    public DateTimeOffset TimestampUtc { get; }

    public string? CorrelationId { get; }

    public Exception? Exception { get; }

    public IReadOnlyDictionary<string, object?>? Properties { get; }

    private LogEntry(LogLevel level,
        string serviceName,
        string @namespace,
        string category,
        string message,
        DateTimeOffset timestampUtc,
        string? correlationId,
        Exception? exception,
        IReadOnlyDictionary<string, object?>? properties)
    {
        Level = level;
        ServiceName = Guard.Against.NullOrWhiteSpace(serviceName);
        Namespace = Guard.Against.NullOrWhiteSpace(@namespace);
        Message = Guard.Against.NullOrWhiteSpace(message);
        TimestampUtc = timestampUtc;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId;
        Exception = exception;
        Properties = properties;
        Category = Guard.Against.NullOrWhiteSpace(category);
    }

    public static LogEntry Create(
        LogLevel level,
        string serviceName,
        string @namespace,
        string? category,
        string message,
        DateTimeOffset timestampUtc,
        string? correlationId = null,
        Exception? exception = null,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        var normalizedCategory = string.IsNullOrWhiteSpace(category) ? "General" : category;
        return new LogEntry(level, serviceName, @namespace, normalizedCategory, message, timestampUtc, correlationId, exception, properties);
    }

    public static LogEntry Create(
        string serviceName,
        string @namespace,
        string? category,
        string message,
        DateTimeOffset timestampUtc,
        string? correlationId = null,
        Exception? exception = null,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        return Create(LogLevel.Information, serviceName, @namespace, category, message, timestampUtc, correlationId, exception, properties);
    }
}
