namespace Common.SharedKernel.Logging;

public sealed record LoggingOptions
{
    public string ServiceName { get; set; } = "UnknownService";

    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    public LogSinkKind EnabledSinks { get; set; } = LogSinkKind.Console;

    public int BatchSize { get; set; } = 128;

    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(1);

    public int QueueCapacity { get; set; } = 4096;

    public BoundedChannelFullMode QueueFullMode { get; set; } = BoundedChannelFullMode.DropWrite;

    public bool CaptureActivityContext { get; set; } = true;

    public HashSet<string> EnabledLogTypes { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "api",
        "trace",
        "error"
    };

    public Action<Exception, string>? SinkFailureCallback { get; set; }
}
