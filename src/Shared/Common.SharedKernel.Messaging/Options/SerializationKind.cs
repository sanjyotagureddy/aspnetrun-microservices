namespace Common.SharedKernel.Messaging;

public enum SerializationKind
{
    SystemTextJson = 0,
    NewtonsoftJson = 1,
    Protobuf = 2,
    Avro = 3
}
