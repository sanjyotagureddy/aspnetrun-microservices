namespace Common.SharedKernel.Messaging;

public interface IMessageEnvelope<out T>
{
    string MessageId { get; }

    string? CorrelationId { get; }

    string? CausationId { get; }

    string? TenantId { get; }

    string Topic { get; }

    DateTimeOffset TimestampUtc { get; }

    IReadOnlyDictionary<string, string> Headers { get; }

    T Payload { get; }
}
