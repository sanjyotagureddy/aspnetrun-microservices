namespace Common.SharedKernel.Messaging;

public interface IMessageHandler<in T>
{
    Task HandleAsync(
        T message,
        IMessageContext context,
        CancellationToken cancellationToken = default);
}
