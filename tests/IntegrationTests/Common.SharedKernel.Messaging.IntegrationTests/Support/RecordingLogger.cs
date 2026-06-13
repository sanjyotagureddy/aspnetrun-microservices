using Common.SharedKernel.Logging;

namespace Common.SharedKernel.Messaging.IntegrationTests.Support;

internal sealed class RecordingLogger<TCategoryName> : ILogger<TCategoryName>
{
    public string Namespace => typeof(TCategoryName).FullName ?? typeof(TCategoryName).Name;

    public List<(TraceLog Log, LogType Destination)> TraceEntries { get; } = [];

    public List<(ErrorLog Log, LogType Destination)> ErrorEntries { get; } = [];

    public ValueTask LogTraceAsync(TraceLog log, LogType logType = LogType.Application, CancellationToken cancellationToken = default)
    {
        TraceEntries.Add((log, logType));
        return ValueTask.CompletedTask;
    }

    public ValueTask LogApiAsync(ApiLog log, LogType logType = LogType.Application, CancellationToken cancellationToken = default)
        => ValueTask.CompletedTask;

    public ValueTask LogErrorAsync(ErrorLog log, LogType logType = LogType.Application, CancellationToken cancellationToken = default)
    {
        ErrorEntries.Add((log, logType));
        return ValueTask.CompletedTask;
    }
}