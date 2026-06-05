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

    ValueTask LogErrorAsync(Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default);
    ValueTask LogErrorAsync(string message, string? category = "exception", Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default);

    ValueTask LogCriticalAsync(Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default);
    ValueTask LogCriticalAsync(string message, string? category = "fatal", Exception? exception = null, IReadOnlyDictionary<string, object?>? properties = null, CancellationToken cancellationToken = default);
}
