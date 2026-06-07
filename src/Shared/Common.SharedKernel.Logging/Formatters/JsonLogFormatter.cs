namespace Common.SharedKernel.Logging;

internal sealed class JsonLogFormatter : ILogFormatter
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public string Format(LogEntry entry)
    {
        Dictionary<string, object?> payload = new(StringComparer.OrdinalIgnoreCase)
        {
            ["timestampUtc"] = entry.TimestampUtc,
            ["level"] = entry.Level.ToString(),
            ["serviceName"] = entry.ServiceName,
            ["category"] = entry.Category,
            ["message"] = entry.Message,
            ["namespace"] = entry.Namespace,
            ["correlationId"] = entry.CorrelationId
        };

        if (entry.Properties is not null)
        {
            foreach (KeyValuePair<string, object?> property in entry.Properties)
            {
                // Keep envelope fields authoritative; flatten custom fields at the top level.
                if (!payload.ContainsKey(property.Key))
                {
                    payload[property.Key] = property.Value;
                }
            }
        }

        if (entry.Exception is not null)
        {
            payload["exception"] = new
            {
                type = entry.Exception.GetType().FullName,
                entry.Exception.Message,
                stackTrace = entry.Exception.StackTrace
            };
        }

        return JsonSerializer.Serialize(payload, Options);
    }
}
