namespace Common.SharedKernel.Logging;

public sealed class LogEnrichmentContext
{
    private readonly Dictionary<string, object?> _properties;

    internal LogEnrichmentContext(
        LogLevel level,
        string @namespace,
        string category,
        string message,
        DateTimeOffset timestampUtc,
        string serviceName,
        LogContext? context,
        Exception? exception,
        IReadOnlyDictionary<string, object?>? properties)
    {
        Level = level;
        Category = category;
        NameSpace = @namespace;
        Message = message;
        TimestampUtc = timestampUtc;
        ServiceName = serviceName;
        Context = context;
        Exception = exception;
        this._properties = properties is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(properties, StringComparer.OrdinalIgnoreCase);
    }

    public LogLevel Level { get; }

    public string Category { get; }
    public string NameSpace { get; }

    public string Message { get; }

    public DateTimeOffset TimestampUtc { get; }

    public string ServiceName { get; }

    public LogContext? Context { get; }

    public Exception? Exception { get; }

    public IReadOnlyDictionary<string, object?> Properties => _properties;

    public void SetProperty(string key, object? value)
    {
        Guard.Against.NullOrWhiteSpace(key);
        _properties[key] = value;
    }

    internal IReadOnlyDictionary<string, object?> ToReadOnlyDictionary() => _properties;
}
