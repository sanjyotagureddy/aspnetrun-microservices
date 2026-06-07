namespace Common.SharedKernel.Logging;

public interface IPayloadStore
{
    Task<PayloadStoreWriteResult> StoreAsync(PayloadStoreWriteRequest request, CancellationToken cancellationToken = default);
}
