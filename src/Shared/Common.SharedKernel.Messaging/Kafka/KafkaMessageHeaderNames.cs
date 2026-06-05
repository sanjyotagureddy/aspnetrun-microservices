namespace Common.SharedKernel.Messaging;

internal static class KafkaMessageHeaderNames
{
    public const string MessageId = "message-id";
    public const string CorrelationId = "correlation-id";
    public const string CausationId = "causation-id";
    public const string TraceId = "trace-id";
    public const string SpanId = "span-id";
    public const string TenantId = "tenant-id";
    public const string ContentType = "content-type";
}
