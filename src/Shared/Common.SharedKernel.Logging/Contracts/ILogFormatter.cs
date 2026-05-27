namespace Common.SharedKernel.Logging;

public interface ILogFormatter
{
    string Format(LogEntry entry);
}
