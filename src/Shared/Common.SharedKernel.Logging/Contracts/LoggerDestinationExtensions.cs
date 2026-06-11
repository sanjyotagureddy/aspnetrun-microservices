namespace Common.SharedKernel.Logging;

public static class LoggerDestinationExtensions
{
    public static ValueTask LogApplicationAsync(this ILogger logger, BaseLog log, CancellationToken cancellationToken = default)
        => LogByDestinationAsync(logger, log, LogType.Application, cancellationToken);

    public static ValueTask LogEventAsync(this ILogger logger, BaseLog log, CancellationToken cancellationToken = default)
        => LogByDestinationAsync(logger, log, LogType.Event, cancellationToken);

    public static ValueTask LogAuditAsync(this ILogger logger, BaseLog log, CancellationToken cancellationToken = default)
        => LogByDestinationAsync(logger, log, LogType.Audit, cancellationToken);

    public static ValueTask LogSecurityAsync(this ILogger logger, BaseLog log, CancellationToken cancellationToken = default)
        => LogByDestinationAsync(logger, log, LogType.Security, cancellationToken);

    public static void LogApplication(this ILogger logger, BaseLog log, CancellationToken cancellationToken = default)
        => LogByDestinationAsync(logger, log, LogType.Application, cancellationToken).GetAwaiter().GetResult();

    public static void LogEvent(this ILogger logger, BaseLog log, CancellationToken cancellationToken = default)
        => LogByDestinationAsync(logger, log, LogType.Event, cancellationToken).GetAwaiter().GetResult();

    public static void LogAudit(this ILogger logger, BaseLog log, CancellationToken cancellationToken = default)
        => LogByDestinationAsync(logger, log, LogType.Audit, cancellationToken).GetAwaiter().GetResult();

    public static void LogSecurity(this ILogger logger, BaseLog log, CancellationToken cancellationToken = default)
        => LogByDestinationAsync(logger, log, LogType.Security, cancellationToken).GetAwaiter().GetResult();

    private static ValueTask LogByDestinationAsync(ILogger logger, BaseLog log, LogType destination, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(log);

        return log switch
        {
            TraceLog trace => logger.LogTraceAsync(trace, destination, cancellationToken),
            ApiLog api => logger.LogApiAsync(api, destination, cancellationToken),
            ErrorLog error => logger.LogErrorAsync(error, destination, cancellationToken),
            _ => throw new ArgumentException($"Unsupported log model type '{log.GetType().Name}'.", nameof(log))
        };
    }
}
