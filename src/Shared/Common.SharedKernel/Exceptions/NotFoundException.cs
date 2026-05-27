namespace Common.SharedKernel.Exceptions;

public sealed class NotFoundException(string resourceName, string resourceKey)
    : BaseApplicationException($"{resourceName} '{resourceKey}' was not found.")
{
    public string ResourceName { get; } = resourceName;

    public string ResourceKey { get; } = resourceKey;
}
