namespace Common.SharedKernel.Messaging.Outbox;

public class OutboxMetadata
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

    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, string> TransportHints { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
