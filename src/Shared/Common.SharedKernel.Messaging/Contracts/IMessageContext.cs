namespace Common.SharedKernel.Messaging;

public interface IMessageContext
{
    string MessageId { get; }

    string? CorrelationId { get; }

    string? TraceId { get; }

    string? SpanId { get; }

    string? TenantId { get; }

    string Topic { get; }

    int? Partition { get; }

    long? Offset { get; }

    IReadOnlyDictionary<string, string> Headers { get; }
}
