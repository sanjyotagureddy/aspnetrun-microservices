namespace Common.SharedKernel.Logging;

public interface IPayloadProtectionPipeline
{
    Task<PayloadProtectionResult> ProtectAsync(PayloadProtectionRequest request, CancellationToken cancellationToken = default);
}
