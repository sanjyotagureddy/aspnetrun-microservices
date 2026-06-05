namespace Common.SharedKernel.Messaging;

public sealed record MessageContext(
    string MessageId,
    string? CorrelationId,
    string? TraceId,
    string? SpanId,
    string? TenantId,
    string Topic,
    int? Partition,
    long? Offset,
    IReadOnlyDictionary<string, string> Headers) : IMessageContext;
