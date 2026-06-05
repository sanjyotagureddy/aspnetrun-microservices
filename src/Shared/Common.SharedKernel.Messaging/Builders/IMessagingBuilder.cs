using Microsoft.Extensions.DependencyInjection;

namespace Common.SharedKernel.Messaging;

public interface IMessagingBuilder
{
    IServiceCollection Services { get; }

    MessagingOptions Options { get; }

    IMessagingBuilder UseSerializer<TSerializer>()
        where TSerializer : class, IMessageSerializer;
}
