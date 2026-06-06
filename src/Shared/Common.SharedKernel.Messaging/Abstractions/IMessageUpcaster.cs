namespace Common.SharedKernel.Messaging;

public interface IMessageUpcaster
{
    bool CanUpcast(MessageContractDescriptor sourceContract, MessageContractDescriptor targetContract, Type payloadType);

    IMessageEnvelope<T> Upcast<T>(IMessageEnvelope<T> envelope, MessageContractDescriptor targetContract);
}
