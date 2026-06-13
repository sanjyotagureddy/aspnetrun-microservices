using System.Text.Json;

namespace Common.SharedKernel.Messaging.Outbox;

public static class OutboxPublisherHelpers
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static T DeserializePayload<T>(string payloadJson)
    {
        T? payload = JsonSerializer.Deserialize<T>(payloadJson, JsonOptions);
        return payload ?? throw new InvalidOperationException($"Unable to deserialize payload for '{typeof(T).Name}'.");
    }

    public static TMetadata DeserializeMetadata<TMetadata>(string metadataJson)
        where TMetadata : OutboxMetadata, new()
    {
        TMetadata? metadata = JsonSerializer.Deserialize<TMetadata>(metadataJson, JsonOptions);
        return metadata ?? new TMetadata();
    }

    public static void CopyMetadata(OutboxMetadata source, MessageMetadata destination)
    {
        destination.MessageId = source.MessageId;
        destination.CorrelationId = source.CorrelationId;
        destination.CausationId = source.CausationId;
        destination.TraceId = source.TraceId;
        destination.SpanId = source.SpanId;
        destination.TenantId = source.TenantId;
        destination.RoutingKey = source.RoutingKey;
        destination.OrderingKey = source.OrderingKey;
        destination.Contract = new MessageContractDescriptor(
            source.Contract.MessageType,
            source.Contract.Version,
            source.Contract.ContentType,
            source.Contract.SchemaRef,
            source.Contract.Compatibility);

        foreach (KeyValuePair<string, string> header in source.Headers)
        {
            destination.Headers[header.Key] = header.Value;
        }

        foreach (KeyValuePair<string, string> hint in source.TransportHints)
        {
            destination.TransportHints[hint.Key] = hint.Value;
        }
    }
}
