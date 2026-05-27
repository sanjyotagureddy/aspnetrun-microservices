namespace Common.SharedKernel.Logging;

public interface ILoggingFactory
{
    ILogger CreateLogger(string category);
}
