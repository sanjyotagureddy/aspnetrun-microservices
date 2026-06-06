namespace Common.SharedKernel.Messaging;

public sealed class MessageMetadata
{
    public string MessageId { get; set; } = Guid.NewGuid().ToString("N");

    public string? CorrelationId { get; set; }

    public string? CausationId { get; set; }

    public string? TraceId { get; set; }

    public string? SpanId { get; set; }

    public string? TenantId { get; set; }

    public string? RoutingKey { get; set; }

    public string? OrderingKey { get; set; }

    public MessageContractDescriptor Contract { get; set; } = MessageContractDescriptor.Unspecified;

    public IDictionary<string, string> TransportHints { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

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
            RoutingKey = RoutingKey,
            OrderingKey = OrderingKey,
            Contract = Contract
        };

        foreach (KeyValuePair<string, string> transportHint in TransportHints)
        {
            clone.TransportHints[transportHint.Key] = transportHint.Value;
        }

        foreach (KeyValuePair<string, string> header in Headers)
        {
            clone.Headers[header.Key] = header.Value;
        }

        return clone;
    }
}
