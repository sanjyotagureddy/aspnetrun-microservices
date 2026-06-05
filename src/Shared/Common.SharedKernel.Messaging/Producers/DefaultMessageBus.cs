namespace Common.SharedKernel.Messaging;

internal sealed class DefaultMessageBus(IMessageProducer producer) : IMessageBus
{
    public Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
        => PublishAsync(topic, message, null, cancellationToken);

    public Task PublishAsync<T>(
        string topic,
        T message,
        Action<MessageMetadata>? configureMetadata,
        CancellationToken cancellationToken = default)
    {
        MessageMetadata metadata = new();
        configureMetadata?.Invoke(metadata);
        return producer.PublishAsync(topic, message, metadata, cancellationToken);
    }

    public Task PublishBatchAsync<T>(string topic, IReadOnlyCollection<T> messages, CancellationToken cancellationToken = default)
        => PublishBatchAsync(topic, messages, null, cancellationToken);

    public Task PublishBatchAsync<T>(
        string topic,
        IReadOnlyCollection<T> messages,
        Action<MessageMetadata>? configureMetadata,
        CancellationToken cancellationToken = default)
    {
        MessageMetadata metadata = new();
        configureMetadata?.Invoke(metadata);
        return producer.PublishBatchAsync(topic, messages, metadata, cancellationToken);
    }
}
