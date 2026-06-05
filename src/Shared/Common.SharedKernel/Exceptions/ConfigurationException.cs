namespace Common.SharedKernel.Exceptions;

public sealed class ConfigurationException(string configurationName, string? reason = null)
    : BaseApplicationException(
        reason is null
            ? $"Configuration '{configurationName}' is invalid or missing."
            : $"Configuration '{configurationName}' is invalid. Reason: {reason}")
{
    public string ConfigurationName { get; } = configurationName;

    public string? Reason { get; } = reason;
}
