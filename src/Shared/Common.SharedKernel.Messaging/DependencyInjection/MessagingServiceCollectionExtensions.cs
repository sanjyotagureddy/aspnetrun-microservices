using Common.SharedKernel.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Common.SharedKernel.Messaging;

public static class MessagingServiceCollectionExtensions
{
    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        Action<IMessagingBuilder> configure)
    {
        Guard.Against.Null(services);
        Guard.Against.Null(configure);

        MessagingOptions options = new();
        MessagingBuilder builder = new(services, options);
        configure(builder);

        services.AddOptions<MessagingOptions>().Configure(configured =>
        {
            configured.Provider = options.Provider;
            configured.TopicPrefix = options.TopicPrefix;
            configured.Serialization = options.Serialization;
            configured.RetryPolicy = options.RetryPolicy;
            configured.DeadLetter = options.DeadLetter;
            configured.Kafka = options.Kafka;
        });

        if (services.All(descriptor => descriptor.ServiceType != typeof(IMessageSerializer)))
        {
            services.AddSingleton<IMessageSerializer, SystemTextJsonMessageSerializer>();
        }

        services.AddSingleton<MessagingInstrumentation>();
        services.AddSingleton<IMessageBus, DefaultMessageBus>();
        services.AddSingleton<IMessageProducer>(provider => provider.GetRequiredService<IMessagingProvider>().CreateProducer());
        services.AddSingleton<IMessageConsumer>(provider => provider.GetRequiredService<IMessagingProvider>().CreateConsumer());
        services.AddSingleton<IMessagingProvider>(provider =>
        {
            MessagingOptions configured = provider.GetRequiredService<IOptions<MessagingOptions>>().Value;
            return configured.Provider switch
            {
                MessagingProviderKind.Kafka => ActivatorUtilities.CreateInstance<KafkaMessagingProvider>(provider),
                _ => throw new MessagingConfigurationException("A messaging provider must be configured.")
            };
        });

        return services;
    }

    public static IMessagingBuilder UseKafka(
        this IMessagingBuilder builder,
        Action<KafkaMessagingOptions>? configure = null)
    {
        Guard.Against.Null(builder);

        builder.Options.Provider = MessagingProviderKind.Kafka;
        configure?.Invoke(builder.Options.Kafka);
        return builder;
    }
}
