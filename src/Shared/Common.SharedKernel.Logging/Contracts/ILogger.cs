namespace Common.SharedKernel.Logging;

public interface ILogger
{
    string Namespace { get; }

    ValueTask LogTraceAsync(TraceLog log, LogType logType = LogType.Application, CancellationToken cancellationToken = default);

    ValueTask LogApiAsync(ApiLog log, LogType logType = LogType.Application, CancellationToken cancellationToken = default);

    ValueTask LogErrorAsync(ErrorLog log, LogType logType = LogType.Application, CancellationToken cancellationToken = default);

    void LogTrace(TraceLog log, LogType logType = LogType.Application, CancellationToken cancellationToken = default)
        => LogTraceAsync(log, logType, cancellationToken).GetAwaiter().GetResult();

    void LogApi(ApiLog log, LogType logType = LogType.Application, CancellationToken cancellationToken = default)
        => LogApiAsync(log, logType, cancellationToken).GetAwaiter().GetResult();

    void LogError(ErrorLog log, LogType logType = LogType.Application, CancellationToken cancellationToken = default)
        => LogErrorAsync(log, logType, cancellationToken).GetAwaiter().GetResult();
}
