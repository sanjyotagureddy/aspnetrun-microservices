namespace Common.SharedKernel.Messaging;

public interface IDestinationProvisioner
{
    Task EnsureAsync(
        IReadOnlyCollection<DestinationRegistration> destinations,
        ProvisioningMode mode,
        CancellationToken cancellationToken = default);
}
