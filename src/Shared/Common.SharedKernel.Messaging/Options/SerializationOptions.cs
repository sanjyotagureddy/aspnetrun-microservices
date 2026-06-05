namespace Common.SharedKernel.Messaging;

public sealed class SerializationOptions
{
    public SerializationKind Kind { get; set; } = SerializationKind.SystemTextJson;
}
