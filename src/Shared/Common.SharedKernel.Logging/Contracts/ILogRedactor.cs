namespace Common.SharedKernel.Logging;

public interface ILogRedactor
{
    LogEntry Redact(LogEntry entry);
}