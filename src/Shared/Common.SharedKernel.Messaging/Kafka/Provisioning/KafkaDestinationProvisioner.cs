using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace Common.SharedKernel.Messaging;

internal sealed class KafkaDestinationProvisioner(IOptions<MessagingOptions> options) : IDestinationProvisioner
{
    private readonly MessagingOptions _options = options.Value;

    public async Task EnsureAsync(
        IReadOnlyCollection<DestinationRegistration> destinations,
        ProvisioningMode mode,
        CancellationToken cancellationToken = default)
    {
        if (destinations.Count == 0)
        {
            return;
        }

        using IAdminClient admin = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = _options.Kafka.BootstrapServers
        }).Build();

        foreach (DestinationRegistration destination in destinations)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (destination.Kind != DestinationKind.Topic)
            {
                throw new MessagingConfigurationException($"Kafka provider supports only topic destinations. Destination '{destination.DestinationName}' uses '{destination.Kind}'.");
            }

            string topic = ResolveTopic(destination.DestinationName);
            bool exists = TopicExists(admin, topic);

            if (!exists)
            {
                if (mode == ProvisioningMode.ValidateOnly)
                {
                    throw new MessagingConfigurationException($"Kafka topic '{topic}' does not exist.");
                }

                await CreateTopicAsync(admin, destination, topic, cancellationToken);
            }

            await ValidateOrReconcileTopicAsync(admin, destination, topic, mode, cancellationToken);
        }
    }

    private async Task CreateTopicAsync(
        IAdminClient admin,
        DestinationRegistration destination,
        string topic,
        CancellationToken cancellationToken)
    {
        short replicationFactor = 1;
        int partitions = Math.Max(1, destination.PartitionCount ?? 1);

        TopicSpecification specification = new()
        {
            Name = topic,
            NumPartitions = partitions,
            ReplicationFactor = replicationFactor,
            Configs = BuildTopicConfigs(destination)
        };

        try
        {
            await admin.CreateTopicsAsync([specification]);
        }
        catch (CreateTopicsException ex)
        {
            bool alreadyExists = ex.Results.All(result => result.Error.Code == ErrorCode.TopicAlreadyExists);
            if (!alreadyExists)
            {
                throw new MessagingConfigurationException($"Failed to create topic '{topic}': {ex.Message}");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task ValidateOrReconcileTopicAsync(
        IAdminClient admin,
        DestinationRegistration destination,
        string topic,
        ProvisioningMode mode,
        CancellationToken cancellationToken)
    {
        TopicMetadata metadata = GetTopicMetadata(admin, topic);

        if (destination.PartitionCount.HasValue)
        {
            int actualPartitions = metadata.Partitions.Count;
            int requiredPartitions = destination.PartitionCount.Value;

            if (actualPartitions < requiredPartitions && mode == ProvisioningMode.ReconcileNonBreaking)
            {
                await admin.CreatePartitionsAsync([
                    new PartitionsSpecification
                    {
                        Topic = topic,
                        IncreaseTo = requiredPartitions
                    }
                ]);

                metadata = GetTopicMetadata(admin, topic);
                actualPartitions = metadata.Partitions.Count;
            }

            if (actualPartitions != requiredPartitions)
            {
                throw new MessagingConfigurationException(
                    $"Kafka topic '{topic}' partition drift detected. Expected {requiredPartitions}, actual {actualPartitions}.");
            }
        }

        if (destination.Retention.HasValue)
        {
            long expectedRetentionMs = (long)destination.Retention.Value.TotalMilliseconds;
            long? actualRetentionMs = await GetRetentionMsAsync(admin, topic);

            if (actualRetentionMs.HasValue && actualRetentionMs.Value != expectedRetentionMs)
            {
                throw new MessagingConfigurationException(
                    $"Kafka topic '{topic}' retention drift detected. Expected {expectedRetentionMs}ms, actual {actualRetentionMs.Value}ms.");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    private TopicMetadata GetTopicMetadata(IAdminClient admin, string topic)
    {
        Metadata metadata = admin.GetMetadata(topic, TimeSpan.FromSeconds(10));
        TopicMetadata? topicMetadata = metadata.Topics.FirstOrDefault(candidate =>
            string.Equals(candidate.Topic, topic, StringComparison.OrdinalIgnoreCase));

        if (topicMetadata is null || topicMetadata.Error.IsError)
        {
            throw new MessagingConfigurationException($"Unable to read metadata for topic '{topic}'.");
        }

        return topicMetadata;
    }

    private static bool TopicExists(IAdminClient admin, string topic)
    {
        Metadata metadata = admin.GetMetadata(topic, TimeSpan.FromSeconds(10));
        TopicMetadata? topicMetadata = metadata.Topics.FirstOrDefault(candidate =>
            string.Equals(candidate.Topic, topic, StringComparison.OrdinalIgnoreCase));

        return topicMetadata is not null && !topicMetadata.Error.IsError;
    }

    private static Dictionary<string, string> BuildTopicConfigs(DestinationRegistration destination)
    {
        Dictionary<string, string> configs = new(StringComparer.OrdinalIgnoreCase);

        if (destination.Retention.HasValue)
        {
            configs["retention.ms"] = ((long)destination.Retention.Value.TotalMilliseconds).ToString(CultureInfo.InvariantCulture);
        }

        return configs;
    }

    private static async Task<long?> GetRetentionMsAsync(IAdminClient admin, string topic)
    {
        ConfigResource resource = new()
        {
            Name = topic,
            Type = ResourceType.Topic
        };

        List<DescribeConfigsResult> results = await admin.DescribeConfigsAsync([resource]);
        DescribeConfigsResult? describeResult = results.FirstOrDefault();

        if (describeResult is null)
        {
            return null;
        }

        describeResult.Entries.TryGetValue("retention.ms", out ConfigEntryResult? retention);

        if (retention is null || string.IsNullOrWhiteSpace(retention.Value))
        {
            return null;
        }

        return long.TryParse(retention.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long parsed)
            ? parsed
            : null;
    }

    private string ResolveTopic(string destinationName)
        => string.IsNullOrWhiteSpace(_options.TopicPrefix)
            ? destinationName
            : $"{_options.TopicPrefix}.{destinationName}";
}
