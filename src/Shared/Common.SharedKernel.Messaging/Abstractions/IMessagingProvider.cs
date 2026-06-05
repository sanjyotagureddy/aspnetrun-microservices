using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Common.SharedKernel.Messaging;

public interface IMessagingProvider
{
    string Name { get; }

    IMessageProducer CreateProducer();

    IMessageConsumer CreateConsumer();

    Task EnsureTopicAsync(string topic, CancellationToken cancellationToken = default);

    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}
