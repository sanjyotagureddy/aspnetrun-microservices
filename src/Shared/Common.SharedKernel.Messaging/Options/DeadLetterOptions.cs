namespace Common.SharedKernel.Messaging;

public sealed class DeadLetterOptions
{
    public bool Enabled { get; set; } = true;

    public string TopicSuffix { get; set; } = ".dlq";
}
