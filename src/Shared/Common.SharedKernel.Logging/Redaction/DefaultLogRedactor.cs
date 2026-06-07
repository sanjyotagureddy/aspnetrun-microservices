namespace Common.SharedKernel.Logging;

internal sealed class DefaultLogRedactor(IOptions<LoggingPolicyOptions> policyOptions) : ILogRedactor
{
    private const string RedactedValue = "***";

    public LogEntry Redact(LogEntry entry)
    {
        LoggingPolicyOptions policy = policyOptions.Value;
        if (!policy.EnableRedaction)
        {
            return entry;
        }

        if (entry.Properties is null || entry.Properties.Count is 0)
        {
            return entry;
        }

        Dictionary<string, object?> redactedProperties = new(StringComparer.OrdinalIgnoreCase);
        bool hasChanges = false;

        foreach (KeyValuePair<string, object?> property in entry.Properties)
        {
            if (policy.SensitiveKeys.Contains(property.Key))
            {
                redactedProperties[property.Key] = RedactedValue;
                hasChanges = true;
                continue;
            }

            redactedProperties[property.Key] = property.Value;
        }

        if (!hasChanges)
        {
            return entry;
        }

        return LogEntry.Create(
            entry.Level,
            entry.ServiceName,
            entry.Namespace,
            entry.Category,
            entry.Message,
            entry.TimestampUtc,
            entry.CorrelationId,
            entry.Exception,
            redactedProperties);
    }
}