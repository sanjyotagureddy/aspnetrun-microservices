namespace Common.SharedKernel.Logging;

public interface ILogFilter
{
    bool IsEnabled(LogEntry entry);
}
