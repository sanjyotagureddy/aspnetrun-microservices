namespace Common.SharedKernel.Messaging;

public interface IMessageConsumer
{
    Task SubscribeAsync<T>(
        string topic,
        IMessageHandler<T> handler,
        CancellationToken cancellationToken = default);

    Task UnsubscribeAsync(
        string topic,
        CancellationToken cancellationToken = default);
}
