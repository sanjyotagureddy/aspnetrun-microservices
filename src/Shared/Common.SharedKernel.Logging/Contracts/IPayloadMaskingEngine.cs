namespace Common.SharedKernel.Logging;

public interface IPayloadMaskingEngine
{
    PayloadProtectionResult Apply(PayloadProtectionRequest request, PayloadProtectionOptions options);
}
