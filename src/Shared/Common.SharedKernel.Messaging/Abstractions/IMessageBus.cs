namespace Common.SharedKernel.Messaging;

public interface IMessageBus
{
    Task PublishAsync<T>(
        string topic,
        T message,
        CancellationToken cancellationToken = default);

    Task PublishAsync<T>(
        string topic,
        T message,
        Action<MessageMetadata>? configureMetadata,
        CancellationToken cancellationToken = default);

    Task PublishBatchAsync<T>(
        string topic,
        IReadOnlyCollection<T> messages,
        CancellationToken cancellationToken = default);

    Task PublishBatchAsync<T>(
        string topic,
        IReadOnlyCollection<T> messages,
        Action<MessageMetadata>? configureMetadata,
        CancellationToken cancellationToken = default);
}
