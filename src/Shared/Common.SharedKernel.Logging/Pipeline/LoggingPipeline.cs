namespace Common.SharedKernel.Logging;

internal sealed class LoggingPipeline(
    ILogContextAccessor contextAccessor,
    IOptions<LoggingOptions> options,
    IReadOnlyList<ILogEnricher> enrichers,
    IReadOnlyList<ILogFilter> filters,
    ILogRedactor redactor,
    LogDispatcher dispatcher,
    TimeProvider timeProvider)
{
    public ValueTask LogAsync(
        string @namespace,
        string? category,
        LogLevel level,
        string message,
        Exception? exception = null,
        IReadOnlyDictionary<string, object?>? properties = null,
        CancellationToken cancellationToken = default)
    {
        Guard.Against.NullOrWhiteSpace(message);

        if (level < options.Value.MinimumLevel)
        {
            return ValueTask.CompletedTask;
        }

        if (!IsLogTypeEnabled(level, category, properties, options.Value.EnabledLogTypes))
        {
            return ValueTask.CompletedTask;
        }

        DateTimeOffset timestampUtc = timeProvider.GetUtcNow();
        LogContext? logContext = contextAccessor.Current;

        Dictionary<string, object?> initialProperties = new(StringComparer.OrdinalIgnoreCase);
        if (logContext?.Properties is not null)
        {
            foreach (KeyValuePair<string, object?> property in logContext.Properties)
            {
                initialProperties[property.Key] = property.Value;
            }
        }

        if (properties is not null)
        {
            foreach (KeyValuePair<string, object?> property in properties)
            {
                initialProperties[property.Key] = property.Value;
            }
        }

        EnrichFromActivity(initialProperties, logContext, options.Value.CaptureActivityContext);

        LogEnrichmentContext enrichmentContext = new(
            level,
            @namespace,
            category ?? string.Empty,
            message,
            timestampUtc,
            options.Value.ServiceName,
            logContext,
            exception,
            initialProperties);

        foreach (ILogEnricher enricher in enrichers)
        {
            enricher.Enrich(enrichmentContext);
        }

        IReadOnlyDictionary<string, object?>? enrichedProperties = enrichmentContext.Properties.Count is 0
            ? null
            : new Dictionary<string, object?>(enrichmentContext.Properties, StringComparer.OrdinalIgnoreCase);

        var correlationId = enrichmentContext.Properties.TryGetValue("correlationId", out var correlationValue)
            ? correlationValue?.ToString()
            : logContext?.CorrelationId;

        var entry = LogEntry.Create(
            level,
            options.Value.ServiceName,
            @namespace,
            category,
            message,
            timestampUtc,
            correlationId,
            exception,
            enrichedProperties);

        if (filters.Any(filter => !filter.IsEnabled(entry)))
        {
            return ValueTask.CompletedTask;
        }

        return dispatcher.EnqueueAsync(redactor.Redact(entry), cancellationToken);
    }

    private static bool IsLogTypeEnabled(
        LogLevel level,
        string? category,
        IReadOnlyDictionary<string, object?>? properties,
        ISet<string>? enabledLogTypes)
    {
        if (enabledLogTypes is null || enabledLogTypes.Count is 0)
        {
            return true;
        }

        if (ContainsType(enabledLogTypes, level.ToString()))
        {
            return true;
        }

        if (properties is not null
            && properties.TryGetValue("logCategory", out object? logCategory)
            && ContainsType(enabledLogTypes, logCategory?.ToString()))
        {
            return true;
        }

        if (properties is not null
            && properties.TryGetValue("logType", out object? logType)
            && ContainsType(enabledLogTypes, logType?.ToString()))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            return false;
        }

        string primaryCategoryToken = category.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? string.Empty;

        return ContainsType(enabledLogTypes, primaryCategoryToken);
    }

    private static bool ContainsType(ISet<string> enabledLogTypes, string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        string normalized = token.Trim();
        if (enabledLogTypes.Contains(normalized))
        {
            return true;
        }

        return enabledLogTypes.Contains("all")
               || enabledLogTypes.Contains("*");
    }

    private static void EnrichFromActivity(
        IDictionary<string, object?> properties,
        LogContext? logContext,
        bool captureActivityContext)
    {
        if (!captureActivityContext)
        {
            return;
        }

        Activity? activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(logContext?.TraceId) && !properties.ContainsKey("traceId"))
        {
            properties["traceId"] = activity.TraceId.ToString();
        }

        if (string.IsNullOrWhiteSpace(logContext?.SpanId) && !properties.ContainsKey("spanId"))
        {
            properties["spanId"] = activity.SpanId.ToString();
        }

        if (activity.ParentSpanId != default && !properties.ContainsKey("parentSpanId"))
        {
            properties["parentSpanId"] = activity.ParentSpanId.ToString();
        }
    }
}
