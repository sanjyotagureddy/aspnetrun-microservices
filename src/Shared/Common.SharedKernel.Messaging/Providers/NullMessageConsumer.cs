namespace Common.SharedKernel.Messaging;

internal sealed class NullMessageConsumer : IMessageConsumer
{
    public Task SubscribeAsync<T>(string topic, IMessageHandler<T> handler, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task UnsubscribeAsync(string topic, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
