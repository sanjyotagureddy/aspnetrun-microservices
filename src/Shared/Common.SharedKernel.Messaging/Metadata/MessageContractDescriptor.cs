namespace Common.SharedKernel.Messaging;

public sealed record MessageContractDescriptor(
    string MessageType,
    string Version,
    string ContentType,
    string? SchemaRef = null,
    CompatibilityMode Compatibility = CompatibilityMode.Backward)
{
    public static MessageContractDescriptor Unspecified { get; } =
        new("unspecified", "1.0", "application/json");
}
