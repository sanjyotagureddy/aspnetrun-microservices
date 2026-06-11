namespace Common.SharedKernel.Logging;

internal sealed class Logger(LoggingPipeline pipeline, string @namespace) : ILogger
{
    public string Namespace { get; } = @namespace;

    public ValueTask LogTraceAsync(TraceLog log, LogType logType = LogType.Application, CancellationToken cancellationToken = default)
        => pipeline.LogAsync(
            Namespace,
            log.Category ?? "trace",
            LogLevel.Trace,
            ResolveMessage(log.Message, "Trace log"),
            exception: null,
            properties: BuildTraceProperties(log, logType),
            cancellationToken);

    public ValueTask LogApiAsync(ApiLog log, LogType logType = LogType.Application, CancellationToken cancellationToken = default)
        => pipeline.LogAsync(
            Namespace,
            log.Category ?? "api",
            LogLevel.Information,
            ResolveMessage(log.Message, "API log"),
            exception: null,
            properties: BuildApiProperties(log, logType),
            cancellationToken);

    public ValueTask LogErrorAsync(ErrorLog log, LogType logType = LogType.Application, CancellationToken cancellationToken = default)
    {
        Exception? exception = log.Exception;

        string message = !string.IsNullOrWhiteSpace(log.Message)
            ? log.Message
            : log.ExceptionMessage ?? exception?.Message ?? "Error log";

        return pipeline.LogAsync(
            Namespace,
            log.Category ?? "error",
            LogLevel.Error,
            message,
            exception,
            BuildErrorProperties(log, logType),
            cancellationToken);
    }

    private static IReadOnlyDictionary<string, object?> BuildTraceProperties(TraceLog log, LogType logType)
    {
        Dictionary<string, object?> result = CreateBaseProperties(log, logType, LogCategory.Trace);

        if (!string.IsNullOrWhiteSpace(log.Operation))
        {
            result["operation"] = log.Operation;
        }

        if (log.DurationMs.HasValue)
        {
            result["durationMs"] = log.DurationMs.Value;
        }

        return result;
    }

    private static IReadOnlyDictionary<string, object?> BuildApiProperties(ApiLog log, LogType logType)
    {
        Dictionary<string, object?> result = CreateBaseProperties(log, logType, LogCategory.Api);

        AddIfNotBlank(result, "method", log.Method);
        AddIfNotBlank(result, "path", log.Path);
        AddIfNotBlank(result, "routeTemplate", log.RouteTemplate);
        AddIfNotBlank(result, "url", log.Url);

        if (log.StatusCode.HasValue)
        {
            result["statusCode"] = log.StatusCode.Value;
        }

        if (log.DurationMs.HasValue)
        {
            result["durationMs"] = log.DurationMs.Value;
        }

        if (log.RequestHeaders is not null)
        {
            result["requestHeaders"] = log.RequestHeaders;
        }

        if (log.ResponseHeaders is not null)
        {
            result["responseHeaders"] = log.ResponseHeaders;
        }

        AddIfNotBlank(result, "requestPayloadRef", log.RequestPayloadRef);
        AddIfNotBlank(result, "responsePayloadRef", log.ResponsePayloadRef);

        return result;
    }

    private static IReadOnlyDictionary<string, object?> BuildErrorProperties(ErrorLog log, LogType logType)
    {
        Dictionary<string, object?> result = CreateBaseProperties(log, logType, LogCategory.Error);

        string? exceptionType = log.ExceptionType ?? log.Exception?.GetType().FullName;
        string? exceptionMessage = log.ExceptionMessage ?? log.Exception?.Message;

        AddIfNotBlank(result, "exceptionType", exceptionType);
        AddIfNotBlank(result, "exceptionMessage", exceptionMessage);
        AddIfNotBlank(result, "errorCode", log.ErrorCode);

        return result;
    }

    private static Dictionary<string, object?> CreateBaseProperties(BaseLog log, LogType logType, LogCategory category)
    {
        Dictionary<string, object?> result = log.Context is null
            ? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, object?>(log.Context, StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(log.CorrelationId))
        {
            result["correlationId"] = log.CorrelationId;
        }

        result["logType"] = ToLogTypeValue(logType);
        result["logCategory"] = category.ToString().ToLowerInvariant();
        return result;
    }

    private static string ToLogTypeValue(LogType logType)
        => logType switch
        {
            LogType.Application => "app",
            LogType.Event => "event",
            LogType.Audit => "audit",
            LogType.Security => "security",
            _ => "app"
        };

    private static string ResolveMessage(string message, string fallback)
        => string.IsNullOrWhiteSpace(message) ? fallback : message;

    private static void AddIfNotBlank(IDictionary<string, object?> target, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            target[key] = value;
        }
    }
}

internal sealed class Logger<TCategoryName>(LoggingPipeline pipeline) : ILogger<TCategoryName>
{
    private readonly ILogger _inner = new Logger(pipeline, typeof(TCategoryName).FullName ?? typeof(TCategoryName).Name);

    public string Namespace => _inner.Namespace;

    public ValueTask LogTraceAsync(TraceLog log, LogType logType = LogType.Application, CancellationToken cancellationToken = default)
        => _inner.LogTraceAsync(log, logType, cancellationToken);

    public ValueTask LogApiAsync(ApiLog log, LogType logType = LogType.Application, CancellationToken cancellationToken = default)
        => _inner.LogApiAsync(log, logType, cancellationToken);

    public ValueTask LogErrorAsync(ErrorLog log, LogType logType = LogType.Application, CancellationToken cancellationToken = default)
        => _inner.LogErrorAsync(log, logType, cancellationToken);
}
