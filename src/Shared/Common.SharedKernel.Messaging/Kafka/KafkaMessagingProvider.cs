using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Common.SharedKernel.Messaging;

internal sealed class KafkaMessagingProvider(IServiceProvider services, IOptions<MessagingOptions> options) : IMessagingProvider
{
    public string Name => "Kafka";

    public MessagingProviderCapabilities Capabilities { get; } = new(
        SupportsPartitioning: true,
        SupportsOrderingByKey: true,
        SupportsNativeDeadLetter: false,
        SupportsTransactions: true,
        SupportsDelayedDelivery: false);

    public IMessageProducer CreateProducer()
        => ActivatorUtilities.CreateInstance<KafkaMessageProducer>(services);

    public IMessageConsumer CreateConsumer()
        => ActivatorUtilities.CreateInstance<KafkaMessageConsumer>(services);

    public IDestinationProvisioner CreateProvisioner()
        => ActivatorUtilities.CreateInstance<KafkaDestinationProvisioner>(services);

    public Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        ProducerConfig config = new() { BootstrapServers = options.Value.Kafka.BootstrapServers };
        using IProducer<string, byte[]> producer = new ProducerBuilder<string, byte[]>(config).Build();
        return Task.FromResult(HealthCheckResult.Healthy("Kafka provider is configured."));
    }
}
