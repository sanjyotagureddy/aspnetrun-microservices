namespace Common.SharedKernel.Messaging;

public abstract record MessageBase(Guid MessageId, DateTime OccurredOnUtc) : IMessage
{
    protected MessageBase()
        : this(Guid.NewGuid(), DateTime.UtcNow)
    {
    }
}