namespace Common.SharedKernel.Logging;

public sealed record FileSinkOptions
{
    public string FilePath { get; set; } = Path.Combine("logs", "application.log");

    public LogFormatterKind FormatterKind { get; set; } = LogFormatterKind.Json;

    public bool RollDaily { get; set; } = true;

    public bool RollOnSizeLimit { get; set; } = true;

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024;

    public int RetainedFileCountLimit { get; set; } = 7;
}
