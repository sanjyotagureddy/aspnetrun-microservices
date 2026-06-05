namespace Common.SharedKernel.Messaging;

public sealed class MessageMetadata
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString("N");

    public string? CorrelationId { get; set; }

    public string? CausationId { get; set; }

    public string? TraceId { get; set; }

    public string? SpanId { get; set; }

    public string? TenantId { get; set; }

    public string? Key { get; set; }

    public int? Partition { get; set; }

    public IDictionary<string, string> Headers { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public MessageMetadata Clone()
    {
        MessageMetadata clone = new()
        {
            MessageId = MessageId,
            CorrelationId = CorrelationId,
            CausationId = CausationId,
            TraceId = TraceId,
            SpanId = SpanId,
            TenantId = TenantId,
            Key = Key,
            Partition = Partition
        };

        foreach (KeyValuePair<string, string> header in Headers)
        {
            clone.Headers[header.Key] = header.Value;
        }

        return clone;
    }
}
