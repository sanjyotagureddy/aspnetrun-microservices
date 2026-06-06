using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Common.SharedKernel.Messaging;

public interface IMessagingProvider
{
    string Name { get; }

    MessagingProviderCapabilities Capabilities { get; }

    IMessageProducer CreateProducer();

    IMessageConsumer CreateConsumer();

    IDestinationProvisioner CreateProvisioner();

    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}
