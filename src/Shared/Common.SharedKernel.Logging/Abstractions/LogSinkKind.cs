namespace Common.SharedKernel.Logging;

[Flags]
public enum LogSinkKind
{
    None = 0,
    Console = 1,
    File = 2,
    Elasticsearch = 4,
    LogStore = 8
}
