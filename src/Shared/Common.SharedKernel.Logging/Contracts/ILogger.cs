namespace Common.SharedKernel.Logging;

public interface ILogger
{
    string Namespace { get; }

    ValueTask LogAsync(
        LogLevel level,
        string message,
        string? category = null,
        Exception? exception = null,
        IReadOnlyDictionary<string, object?>? properties = null,
        CancellationToken cancellationToken = default);

    ValueTask LogTraceAsync(string message, string? category = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default);

    ValueTask LogDebugAsync(string message, string? category = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default);

    ValueTask LogInformationAsync(string message, string? category = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default);
    ValueTask LogInformationAsync(string message, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default);

    ValueTask LogWarningAsync(string message, string? category = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default);

    ValueTask LogApiAsync(string message, string? category = "api", IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default);

    ValueTask LogEventAsync(string message, string? category = "event", IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default);

    ValueTask LogAuditAsync(string message, string? category = "audit", IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default);

    ValueTask LogSecurityAsync(string message, string? category = "security", IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default);

    ValueTask LogErrorAsync(Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default);
    ValueTask LogErrorAsync(string message, string? category = "exception", Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default);

    ValueTask LogCriticalAsync(Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default);
    ValueTask LogCriticalAsync(string message, string? category = "fatal", Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default);

    void Log(
        LogLevel level,
        string message,
        string? category = null,
        Exception? exception = null,
        IReadOnlyDictionary<string, object?>? properties = null,
        CancellationToken cancellationToken = default)
        => LogAsync(level, message, category, exception, properties, cancellationToken).GetAwaiter().GetResult();

    void LogTrace(string message, string? category = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogTraceAsync(message, category, properties, cancellationToken).GetAwaiter().GetResult();

    void LogDebug(string message, string? category = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogDebugAsync(message, category, properties, cancellationToken).GetAwaiter().GetResult();

    void LogInformation(string message, string? category = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogInformationAsync(message, category, properties, cancellationToken).GetAwaiter().GetResult();

    void LogInformation(string message, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogInformationAsync(message, properties, cancellationToken).GetAwaiter().GetResult();

    void LogWarning(string message, string? category = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogWarningAsync(message, category, properties, cancellationToken).GetAwaiter().GetResult();

    void LogApi(string message, string? category = "api", IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogApiAsync(message, category, properties, cancellationToken).GetAwaiter().GetResult();

    void LogEvent(string message, string? category = "event", IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogEventAsync(message, category, properties, cancellationToken).GetAwaiter().GetResult();

    void LogAudit(string message, string? category = "audit", IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogAuditAsync(message, category, properties, cancellationToken).GetAwaiter().GetResult();

    void LogSecurity(string message, string? category = "security", IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogSecurityAsync(message, category, properties, cancellationToken).GetAwaiter().GetResult();

    void LogError(Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogErrorAsync(exception, properties, cancellationToken).GetAwaiter().GetResult();

    void LogError(string message, string? category = "exception", Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogErrorAsync(message, category, exception, properties, cancellationToken).GetAwaiter().GetResult();

    void LogCritical(Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogCriticalAsync(exception, properties, cancellationToken).GetAwaiter().GetResult();

    void LogCritical(string message, string? category = "fatal", Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default)
        => LogCriticalAsync(message, category, exception, properties, cancellationToken).GetAwaiter().GetResult();
}
