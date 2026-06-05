namespace Common.SharedKernel.Messaging;

public sealed class MessagingOptions
{
    public MessagingProviderKind Provider { get; set; } = MessagingProviderKind.None;

    public string TopicPrefix { get; set; } = string.Empty;

    public SerializationOptions Serialization { get; set; } = new();

    public RetryPolicyOptions RetryPolicy { get; set; } = new();

    public DeadLetterOptions DeadLetter { get; set; } = new();

    public KafkaMessagingOptions Kafka { get; set; } = new();
}
