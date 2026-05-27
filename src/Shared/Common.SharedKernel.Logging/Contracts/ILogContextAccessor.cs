namespace Common.SharedKernel.Logging;

public interface ILogContextAccessor
{
    LogContext? Current { get; }

    IDisposable BeginScope(LogContext context);
}
