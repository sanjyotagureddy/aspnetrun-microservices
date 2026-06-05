namespace Common.SharedKernel.Messaging;

public interface IMessageSerializer
{
    string ContentType { get; }

    byte[] Serialize<T>(IMessageEnvelope<T> envelope);

    IMessageEnvelope<T> Deserialize<T>(ReadOnlySpan<byte> payload);
}
