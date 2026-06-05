namespace Common.SharedKernel.Messaging;

public interface IMessageProducer
{
    Task PublishAsync<T>(
        string topic,
        T message,
        MessageMetadata metadata,
        CancellationToken cancellationToken = default);

    Task PublishBatchAsync<T>(
        string topic,
        IReadOnlyCollection<T> messages,
        MessageMetadata metadata,
        CancellationToken cancellationToken = default);
}
