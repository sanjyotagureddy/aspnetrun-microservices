namespace Common.SharedKernel.Logging;

internal sealed class Logger(LoggingPipeline pipeline, string @namespace) : ILogger
{
    public string Namespace { get; } = @namespace;

    public ValueTask LogAsync(
        LogLevel level,
        string message,
        string? category = null,
        Exception? exception = null,
        IReadOnlyDictionary<string, object?>? properties = null,
        CancellationToken cancellationToken = default)
        => pipeline.LogAsync(Namespace, category, level, message, exception, properties, cancellationToken);

    public ValueTask LogTraceAsync(string message, string? category = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogAsync(LogLevel.Trace, message, category, null, properties, cancellationToken);

    public ValueTask LogDebugAsync(string message, string? category = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogAsync(LogLevel.Debug, message, category, null, properties, cancellationToken);

    public ValueTask LogInformationAsync(string message, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogAsync(LogLevel.Information, message, null, null, properties, cancellationToken);
    public ValueTask LogInformationAsync(string message, string? category = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
            => LogAsync(LogLevel.Information, message, category, null, properties, cancellationToken);

    public ValueTask LogWarningAsync(string message, string? category = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogAsync(LogLevel.Warning, message, category, null, properties, cancellationToken);

    public ValueTask LogApiAsync(string message, string? category = "api", IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogAsync(LogLevel.Information, message, category, null, EnsureLogType(properties, "api"), cancellationToken);

    public ValueTask LogEventAsync(string message, string? category = "event", IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogAsync(LogLevel.Information, message, category, null, EnsureLogType(properties, "event"), cancellationToken);

    public ValueTask LogAuditAsync(string message, string? category = "audit", IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogAsync(LogLevel.Information, message, category, null, EnsureLogType(properties, "audit"), cancellationToken);

    public ValueTask LogSecurityAsync(string message, string? category = "security", IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogAsync(LogLevel.Warning, message, category, null, EnsureLogType(properties, "security"), cancellationToken);

    public ValueTask LogErrorAsync(string message, string? category = null, Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogAsync(LogLevel.Error, message, category, exception, EnsureLogType(properties, "error"), cancellationToken);

    public ValueTask LogCriticalAsync(string message, string? category = null, Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogAsync(LogLevel.Critical, message, category, exception, EnsureLogType(properties, "error"), cancellationToken);

    public ValueTask LogErrorAsync(Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogAsync(LogLevel.Error, exception?.Message ?? "An exception occurred", "exception", exception, EnsureLogType(properties, "error"), cancellationToken);

    public ValueTask LogCriticalAsync(Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogAsync(LogLevel.Critical, exception?.Message ?? "A critical error occurred", "fatal", exception, EnsureLogType(properties, "error"), cancellationToken);

    private static IReadOnlyDictionary<string, object?> EnsureLogType(IReadOnlyDictionary<string, object?>? properties, string logType)
    {
        Dictionary<string, object?> result = properties is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(properties, StringComparer.OrdinalIgnoreCase);

        result["logType"] = logType;
        return result;
    }
}

internal sealed class Logger<TCategoryName>(LoggingPipeline pipeline) : ILogger<TCategoryName>
{
    private readonly ILogger _inner = new Logger(pipeline, typeof(TCategoryName).FullName ?? typeof(TCategoryName).Name);

    public string Namespace => _inner.Namespace;

    public ValueTask LogAsync(LogLevel level, string message, string? category = null, Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => _inner.LogAsync(level, message, category, exception, properties, cancellationToken);

    public ValueTask LogTraceAsync(string message, string? category = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => _inner.LogTraceAsync(message, category, properties, cancellationToken);

    public ValueTask LogDebugAsync(string message, string? category = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => _inner.LogDebugAsync(message, category, properties, cancellationToken);

    public ValueTask LogInformationAsync(string message, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => _inner.LogInformationAsync(message, null, properties, cancellationToken);


    public ValueTask LogInformationAsync(string message, string? category = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => _inner.LogInformationAsync(message, category, properties, cancellationToken);

    public ValueTask LogWarningAsync(string message, string? category = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => _inner.LogWarningAsync(message, category, properties, cancellationToken);

    public ValueTask LogApiAsync(string message, string? category = "api", IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => _inner.LogApiAsync(message, category, properties, cancellationToken);

    public ValueTask LogEventAsync(string message, string? category = "event", IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => _inner.LogEventAsync(message, category, properties, cancellationToken);

    public ValueTask LogAuditAsync(string message, string? category = "audit", IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => _inner.LogAuditAsync(message, category, properties, cancellationToken);

    public ValueTask LogSecurityAsync(string message, string? category = "security", IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => _inner.LogSecurityAsync(message, category, properties, cancellationToken);

    public ValueTask LogErrorAsync(Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null,
        CancellationToken cancellationToken = default)
        => _inner.LogErrorAsync(exception?.Message ?? "An exception occurred", "exception", exception, properties, cancellationToken);

    public ValueTask LogErrorAsync(string message, string? category = null, Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => _inner.LogErrorAsync(message, category, exception, properties, cancellationToken);

    public ValueTask LogCriticalAsync(Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null,
        CancellationToken cancellationToken = default)
        => _inner.LogCriticalAsync(exception?.Message ?? "A critical error occurred", "fatal", exception, properties, cancellationToken);

    public ValueTask LogCriticalAsync(string message, string? category = null, Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => _inner.LogCriticalAsync(message, category, exception, properties, cancellationToken);


}
