using System.Diagnostics.CodeAnalysis;

namespace Auth.Api.Infrastructure.Configuration;

internal sealed class DevBootstrapOptions
{
    public bool Enabled { get; set; }
    public string SharedSecret { get; set; } = string.Empty;
}

[ExcludeFromCodeCoverage]
internal static class DevBootstrapOptionsValidation
{
    public static bool IsValidForEnvironment(DevBootstrapOptions options, IHostEnvironment environment)
    {
        if (!options.Enabled)
        {
            return true;
        }

        if (!environment.IsDevelopment())
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(options.SharedSecret);
    }
}
