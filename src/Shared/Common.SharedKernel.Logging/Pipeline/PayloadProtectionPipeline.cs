namespace Common.SharedKernel.Logging;

internal sealed class PayloadProtectionPipeline(
    IPayloadMaskingEngine maskingEngine,
    IOptions<PayloadProtectionOptions> options) : IPayloadProtectionPipeline
{
    public Task<PayloadProtectionResult> ProtectAsync(PayloadProtectionRequest request, CancellationToken cancellationToken = default)
    {
        Guard.Against.Null(request);

        if (!options.Value.Enabled)
        {
            return Task.FromResult(new PayloadProtectionResult(true, request.Payload, 0, 0));
        }

        try
        {
            return Task.FromResult(maskingEngine.Apply(request, options.Value));
        }
        catch (Exception exception)
        {
            PayloadProtectionFailure failure = new(
                "payload_protection_failed",
                exception.Message,
                options.Value.FailureBehavior);

            return Task.FromResult(new PayloadProtectionResult(false, null, 0, 0, failure));
        }
    }
}
