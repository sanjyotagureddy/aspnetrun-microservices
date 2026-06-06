using System.Globalization;

namespace Common.SharedKernel.Messaging;

public static class KafkaMessageMetadataExtensions
{
    public static MessageMetadata SetKafkaPartition(this MessageMetadata metadata, int partition)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        metadata.TransportHints[KafkaTransportHintNames.Partition] = partition.ToString(CultureInfo.InvariantCulture);
        return metadata;
    }

    public static bool TryGetKafkaPartition(this MessageMetadata metadata, out int partition)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        if (metadata.TransportHints.TryGetValue(KafkaTransportHintNames.Partition, out string? raw)
            && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed))
        {
            partition = parsed;
            return true;
        }

        partition = default;
        return false;
    }
}
