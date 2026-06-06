namespace Common.SharedKernel.Messaging;

public interface IContractAwareMessageHandler
{
    string? SupportedMessageType { get; }

    IReadOnlyCollection<string> SupportedContractVersions { get; }
}
