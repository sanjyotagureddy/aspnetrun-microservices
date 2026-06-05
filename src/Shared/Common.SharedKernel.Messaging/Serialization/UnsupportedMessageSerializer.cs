namespace Common.SharedKernel.Messaging;

public sealed class UnsupportedMessageSerializer(string contentType) : IMessageSerializer
{
    public string ContentType { get; } = contentType;

    public byte[] Serialize<T>(IMessageEnvelope<T> envelope)
        => throw new NotSupportedException($"{ContentType} serialization is not implemented yet.");

    public IMessageEnvelope<T> Deserialize<T>(ReadOnlySpan<byte> payload)
        => throw new NotSupportedException($"{ContentType} serialization is not implemented yet.");
}
