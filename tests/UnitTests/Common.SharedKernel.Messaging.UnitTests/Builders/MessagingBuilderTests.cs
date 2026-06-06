using Common.SharedKernel.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Common.SharedKernel.Messaging.UnitTests.Builders;

public sealed class MessagingBuilderTests
{
    [Fact]
    public void AddMessaging_ShouldRegisterKafkaProviderAndDefaultSerializer()
    {
        ServiceCollection services = new();
        services.AddCommonSharedKernelLogging(builder =>
        {
            builder.SetServiceName("MessagingTests");
            builder.UseConsole();
        });

        services.AddMessaging(builder =>
        {
            builder.UseKafka(options =>
            {
                options.BootstrapServers = "localhost:9092";
                options.ConsumerGroup = "orders";
            });
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IMessageBus>().Should().NotBeNull();
        provider.GetRequiredService<IMessageSerializer>().Should().BeOfType<SystemTextJsonMessageSerializer>();
        provider.GetRequiredService<IMessagingProvider>().Name.Should().Be("Kafka");
        provider.GetRequiredService<IOptions<MessagingOptions>>().Value.Kafka.ConsumerGroup.Should().Be("orders");
    }

    [Fact]
    public void AddMessaging_ShouldUseCustomSerializer_WhenConfigured()
    {
        ServiceCollection services = new();
        services.AddCommonSharedKernelLogging(builder =>
        {
            builder.SetServiceName("MessagingTests");
            builder.UseConsole();
        });

        services.AddMessaging(builder =>
        {
            builder.UseSerializer<UnsupportedMessageSerializerForTests>();
            builder.UseKafka();
        });

        using ServiceProvider provider = services.BuildServiceProvider();

        provider.GetRequiredService<IMessageSerializer>().Should().BeOfType<UnsupportedMessageSerializerForTests>();
    }

    private sealed class UnsupportedMessageSerializerForTests : IMessageSerializer
    {
        public string ContentType => "application/test";

        public byte[] Serialize<T>(IMessageEnvelope<T> envelope) => [];

        public IMessageEnvelope<T> Deserialize<T>(ReadOnlySpan<byte> payload)
            => throw new NotSupportedException();
    }
}
