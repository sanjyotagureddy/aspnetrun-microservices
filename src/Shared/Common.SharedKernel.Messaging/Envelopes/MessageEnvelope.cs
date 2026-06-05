namespace Common.SharedKernel.Messaging;

public sealed record MessageEnvelope<T>(
    string MessageId,
    string? CorrelationId,
    string? CausationId,
    string? TenantId,
    string Topic,
    DateTimeOffset TimestampUtc,
    IReadOnlyDictionary<string, string> Headers,
    T Payload) : IMessageEnvelope<T>
{
    public static MessageEnvelope<T> Create(string topic, T payload, MessageMetadata metadata)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(metadata);

        return new MessageEnvelope<T>(
            string.IsNullOrWhiteSpace(metadata.MessageId) ? Guid.NewGuid().ToString("N") : metadata.MessageId,
            metadata.CorrelationId,
            metadata.CausationId,
            metadata.TenantId,
            topic,
            DateTimeOffset.UtcNow,
            new Dictionary<string, string>(metadata.Headers, StringComparer.OrdinalIgnoreCase),
            payload);
    }
}
