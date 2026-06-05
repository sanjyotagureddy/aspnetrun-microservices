using Microsoft.Extensions.DependencyInjection;

namespace Common.SharedKernel.Messaging;

internal sealed class MessagingBuilder(IServiceCollection services, MessagingOptions options) : IMessagingBuilder
{
    public IServiceCollection Services { get; } = services;

    public MessagingOptions Options { get; } = options;

    public IMessagingBuilder UseSerializer<TSerializer>()
        where TSerializer : class, IMessageSerializer
    {
        Services.AddSingleton<IMessageSerializer, TSerializer>();
        return this;
    }
}
