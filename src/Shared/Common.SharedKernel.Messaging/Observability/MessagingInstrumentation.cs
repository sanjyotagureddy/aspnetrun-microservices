using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Common.SharedKernel.Messaging;

internal sealed class MessagingInstrumentation : IDisposable
{
    public const string ActivitySourceName = "Common.SharedKernel.Messaging";
    public const string MeterName = "Common.SharedKernel.Messaging";

    private readonly Meter _meter = new(MeterName);

    public ActivitySource ActivitySource { get; } = new(ActivitySourceName);

    public Histogram<double> PublishDurationMs { get; }

    public Histogram<double> ConsumeDurationMs { get; }

    public Counter<long> PublishFailures { get; }

    public Counter<long> ConsumeFailures { get; }

    public Counter<long> RetryCount { get; }

    public Counter<long> DeadLetterCount { get; }

    public ObservableGauge<long> ConsumerLag { get; }

    private long _consumerLag;

    public MessagingInstrumentation()
    {
        PublishDurationMs = _meter.CreateHistogram<double>("messaging.publish.duration", "ms");
        ConsumeDurationMs = _meter.CreateHistogram<double>("messaging.consume.duration", "ms");
        PublishFailures = _meter.CreateCounter<long>("messaging.publish.failures");
        ConsumeFailures = _meter.CreateCounter<long>("messaging.consume.failures");
        RetryCount = _meter.CreateCounter<long>("messaging.retry.count");
        DeadLetterCount = _meter.CreateCounter<long>("messaging.deadletter.count");
        ConsumerLag = _meter.CreateObservableGauge("messaging.consumer.lag", () => _consumerLag);
    }

    public void SetConsumerLag(long value) => _consumerLag = value;

    public void Dispose()
    {
        ActivitySource.Dispose();
        _meter.Dispose();
    }
}
