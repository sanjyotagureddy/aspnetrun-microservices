namespace Common.SharedKernel.Messaging;

public interface IMessage
{
    Guid MessageId { get; }

    DateTime OccurredOnUtc { get; }
}