using System.Text.Json;

namespace Common.SharedKernel.Messaging;

public sealed class SystemTextJsonMessageSerializer : IMessageSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string ContentType => "application/json";

    public byte[] Serialize<T>(IMessageEnvelope<T> envelope)
        => JsonSerializer.SerializeToUtf8Bytes(envelope, JsonOptions);

    public IMessageEnvelope<T> Deserialize<T>(ReadOnlySpan<byte> payload)
    {
        MessageEnvelope<T>? envelope = JsonSerializer.Deserialize<MessageEnvelope<T>>(payload, JsonOptions);
        if (envelope is null)
        {
            throw new MessagingException("Message envelope could not be deserialized.");
        }

        if (envelope.Contract is null)
        {
            return envelope with { Contract = MessageContractDescriptor.Unspecified };
        }

        return envelope;
    }
}
