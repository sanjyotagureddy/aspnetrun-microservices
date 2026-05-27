namespace Common.SharedKernel.Logging;

internal sealed class FileLogSink(FileSinkOptions options) : ILogSink
{
    private readonly FileSinkOptions _options = Guard.Against.Null(options);
    private readonly ILogFormatter _formatter = options.FormatterKind switch
    {
        LogFormatterKind.Json => new JsonLogFormatter(),
        _ => new TextLogFormatter()
    };
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _currentFilePath;

    public async ValueTask WriteAsync(LogEntry entry, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            string filePath = ResolveFilePath(entry.TimestampUtc);
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            await File.AppendAllTextAsync(filePath, _formatter.Format(entry) + Environment.NewLine, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private string ResolveFilePath(DateTimeOffset timestampUtc)
    {
        string basePath = _options.FilePath;
        if (!_options.RollDaily)
        {
            _currentFilePath ??= basePath;
            return _currentFilePath;
        }

        string directory = Path.GetDirectoryName(basePath) ?? string.Empty;
        string fileName = Path.GetFileNameWithoutExtension(basePath);
        string extension = Path.GetExtension(basePath);
        string rolledName = $"{fileName}-{timestampUtc:yyyyMMdd}{extension}";
        return string.IsNullOrWhiteSpace(directory) ? rolledName : Path.Combine(directory, rolledName);
    }
}
