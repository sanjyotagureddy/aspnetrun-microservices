namespace Common.SharedKernel.Messaging;

public sealed class KafkaMessagingOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";

    public string ConsumerGroup { get; set; } = "default";

    public bool EnableAutoCommit { get; set; }

    public string AutoOffsetReset { get; set; } = "Earliest";

    public TimeSpan ConsumeTimeout { get; set; } = TimeSpan.FromMilliseconds(250);
}
