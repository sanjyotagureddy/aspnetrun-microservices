namespace Common.SharedKernel.Logging;

internal sealed class LoggingPipeline(
    ILogContextAccessor contextAccessor,
    IOptions<LoggingOptions> options,
    IReadOnlyList<ILogEnricher> enrichers,
    IReadOnlyList<ILogFilter> filters,
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

        LogEnrichmentContext enrichmentContext = new(
            level,
            @namespace,
            category,
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

        return filters.Any(filter => !filter.IsEnabled(entry)) ? ValueTask.CompletedTask : dispatcher.EnqueueAsync(entry, cancellationToken);
    }
}
