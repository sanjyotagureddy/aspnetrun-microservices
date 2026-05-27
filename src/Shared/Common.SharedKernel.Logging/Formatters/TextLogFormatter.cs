namespace Common.SharedKernel.Logging;

internal sealed class TextLogFormatter : ILogFormatter
{
    public string Format(LogEntry entry)
    {
        StringBuilder builder = new();
        builder.Append('[')
               .Append(entry.TimestampUtc.ToString("O", CultureInfo.InvariantCulture))
               .Append("] ")
               .Append(entry.Level)
               .Append(' ')
               .Append(entry.ServiceName)
               .Append(" - ")
               .Append(entry.Category)
               .Append(" - ")
               .Append(entry.Message);

        if (!string.IsNullOrWhiteSpace(entry.CorrelationId))
        {
            builder.Append(" correlationId=").Append(entry.CorrelationId);
        }

        if (entry.Exception is not null)
        {
            builder.AppendLine().Append(entry.Exception);
        }

        return builder.ToString();
    }
}
