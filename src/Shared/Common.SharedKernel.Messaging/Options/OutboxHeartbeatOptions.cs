namespace Common.SharedKernel.Messaging;

public sealed class OutboxHeartbeatOptions
{
    public bool Enabled { get; set; }

    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(5);
}
