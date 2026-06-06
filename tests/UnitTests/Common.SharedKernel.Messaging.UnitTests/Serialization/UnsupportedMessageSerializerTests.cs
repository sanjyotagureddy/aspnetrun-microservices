namespace Common.SharedKernel.Messaging.UnitTests.Serialization;

public sealed class UnsupportedMessageSerializerTests
{
    [Fact]
    public void Serialize_ShouldThrowNotSupportedException()
    {
        UnsupportedMessageSerializer serializer = new("application/avro");
        MessageMetadata metadata = new();
        MessageEnvelope<TestPayload> envelope = MessageEnvelope<TestPayload>.Create("products.events.v1", new TestPayload("P-1"), metadata);

        Action act = () => serializer.Serialize(envelope);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*application/avro serialization is not implemented yet.*");
    }

    [Fact]
    public void Deserialize_ShouldThrowNotSupportedException()
    {
        UnsupportedMessageSerializer serializer = new("application/protobuf");

        Action act = () => serializer.Deserialize<TestPayload>(ReadOnlySpan<byte>.Empty);

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*application/protobuf serialization is not implemented yet.*");
    }

    private sealed record TestPayload(string ProductId);
}
