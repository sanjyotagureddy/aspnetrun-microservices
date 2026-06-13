using Microsoft.Extensions.Configuration;

namespace Common.SharedKernel.Messaging;

public static class MessagingBuilderConfigurationExtensions
{
    public static IMessagingBuilder ApplyConfiguration(
        this IMessagingBuilder builder,
        IConfiguration configuration,
        string defaultConsumerGroup)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultConsumerGroup);

        MessagingOptions configured = new();
        configuration.GetSection("Messaging").Bind(configured);

        builder.Options.Provider = configured.Provider is MessagingProviderKind.None
            ? MessagingProviderKind.Kafka
            : configured.Provider;
        builder.Options.TopicPrefix = configured.TopicPrefix;
        builder.Options.ProvisioningMode = configured.ProvisioningMode;
        builder.Options.Serialization = configured.Serialization;
        builder.Options.RetryPolicy = configured.RetryPolicy;
        builder.Options.DeadLetter = configured.DeadLetter;
        builder.Options.OutboxHeartbeat = configured.OutboxHeartbeat;

        if (builder.Options.OutboxHeartbeat.Interval <= TimeSpan.Zero)
        {
            builder.Options.OutboxHeartbeat.Interval = TimeSpan.FromMinutes(5);
        }

        builder.Options.Destinations = configured.Destinations
            .Select(destination => destination.Clone())
            .ToList();

        KafkaMessagingOptions kafka = configured.Kafka;
        kafka.BootstrapServers = configuration.GetConnectionString("message-broker")
                                 ?? configuration["Messaging:Kafka:BootstrapServers"]
                                 ?? kafka.BootstrapServers;

        if (string.IsNullOrWhiteSpace(kafka.ConsumerGroup))
        {
            kafka.ConsumerGroup = defaultConsumerGroup;
        }

        builder.Options.Kafka = kafka;
        return builder;
    }
}
