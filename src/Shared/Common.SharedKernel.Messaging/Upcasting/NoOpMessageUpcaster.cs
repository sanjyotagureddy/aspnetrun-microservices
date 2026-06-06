namespace Common.SharedKernel.Messaging;

internal sealed class NoOpMessageUpcaster : IMessageUpcaster
{
    public bool CanUpcast(MessageContractDescriptor sourceContract, MessageContractDescriptor targetContract, Type payloadType)
        => false;

    public IMessageEnvelope<T> Upcast<T>(IMessageEnvelope<T> envelope, MessageContractDescriptor targetContract)
        => envelope;
}
