using Common.SharedKernel.Helpers;

namespace Common.SharedKernel.Logging;

public sealed record LogEntry
{
    public string Category { get; }

    public string Message { get; }

    public DateTimeOffset TimestampUtc { get; }

    public string? CorrelationId { get; }

    public Exception? Exception { get; }

    public IReadOnlyDictionary<string, object?>? Properties { get; }

    private LogEntry(
        string category,
        string message,
        DateTimeOffset timestampUtc,
        string? correlationId,
        Exception? exception,
        IReadOnlyDictionary<string, object?>? properties)
    {
        Category = Guard.Against.NullOrWhiteSpace(category, nameof(category));
        Message = Guard.Against.NullOrWhiteSpace(message, nameof(message));
        TimestampUtc = timestampUtc;
        CorrelationId = string.IsNullOrWhiteSpace(correlationId) ? null : correlationId;
        Exception = exception;
        Properties = properties;
    }

    public static LogEntry Create(
        string category,
        string message,
        DateTimeOffset timestampUtc,
        string? correlationId = null,
        Exception? exception = null,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        return new LogEntry(category, message, timestampUtc, correlationId, exception, properties);
    }
}