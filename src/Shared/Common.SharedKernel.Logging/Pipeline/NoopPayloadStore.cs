namespace Common.SharedKernel.Logging;

internal sealed class NoopPayloadStore : IPayloadStore
{
    public Task<PayloadStoreWriteResult> StoreAsync(PayloadStoreWriteRequest request, CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(request);

        return Task.FromResult(new PayloadStoreWriteResult(
            PayloadRef: string.Empty,
            PayloadHash: string.Empty,
            PayloadSizeBytes: 0,
            PayloadEncoding: "application/json",
            Compressed: false,
            Encrypted: false));
    }
}
