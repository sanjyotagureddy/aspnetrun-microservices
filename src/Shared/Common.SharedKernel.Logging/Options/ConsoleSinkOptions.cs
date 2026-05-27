namespace Common.SharedKernel.Logging;

public sealed record ConsoleSinkOptions
{
    public LogFormatterKind FormatterKind { get; set; } = LogFormatterKind.Text;

    public bool WriteErrorsToStandardError { get; set; } = true;
}
