namespace Common.SharedKernel.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync(IMessage message, CancellationToken cancellationToken = default);

    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : IMessage;
}