namespace Common.SharedKernel.Messaging;

public sealed class RetryPolicyOptions
{
    public int MaxAttempts { get; set; } = 3;

    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    public double BackoffMultiplier { get; set; } = 2;
}
