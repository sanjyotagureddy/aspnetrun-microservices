namespace Common.SharedKernel.Logging;

internal sealed class LoggingFactory(LoggingPipeline pipeline) : ILoggingFactory
{
    public ILogger CreateLogger(string category)
        => new Logger(pipeline, Guard.Against.NullOrWhiteSpace(category));
}
