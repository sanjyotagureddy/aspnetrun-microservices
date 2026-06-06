namespace Common.SharedKernel.Messaging;

public enum ProvisioningMode
{
    ValidateOnly = 0,
    CreateIfMissing = 1,
    ReconcileNonBreaking = 2
}
