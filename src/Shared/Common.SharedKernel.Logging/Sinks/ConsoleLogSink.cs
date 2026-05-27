namespace Common.SharedKernel.Logging;

internal sealed class ConsoleLogSink : ILogSink
{
    private readonly ILogFormatter _formatter;
    private readonly bool _writeErrorsToStandardError;

    public ConsoleLogSink(ConsoleSinkOptions options)
    {
        Guard.Against.Null(options);
        _formatter = options.FormatterKind switch
        {
            LogFormatterKind.Json => new JsonLogFormatter(),
            _ => new TextLogFormatter()
        };
        _writeErrorsToStandardError = options.WriteErrorsToStandardError;
    }

    public ValueTask WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        string line = _formatter.Format(entry);
        if (_writeErrorsToStandardError && entry.Level >= LogLevel.Error)
        {
            Console.Error.WriteLine(line);
        }
        else
        {
            Console.Out.WriteLine(line);
        }

        return ValueTask.CompletedTask;
    }
}
