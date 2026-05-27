namespace Common.SharedKernel.Logging;

public interface ILogSink
{
    ValueTask WriteAsync(LogEntry entry, CancellationToken cancellationToken = default);
}