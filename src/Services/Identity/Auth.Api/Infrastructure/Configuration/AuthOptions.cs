using System.Diagnostics.CodeAnalysis;

namespace Auth.Api.Infrastructure.Configuration;

internal sealed class AuthOptions
{
    public string Authority { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string DiscoveryUrl { get; set; } = string.Empty;
    public string JwksUrl { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string WebClientId { get; set; } = string.Empty;
    public string WebClientScope { get; set; } = "openid profile email";
    public string? WebClientSecret { get; set; }
    public List<PkceClientOptions> PkceClients { get; set; } = [];
}

internal sealed class PkceClientOptions
{
    public string ClientId { get; set; } = string.Empty;
    public string Scope { get; set; } = "openid profile email";
    public List<string> RedirectUris { get; set; } = [];
}

[ExcludeFromCodeCoverage]
internal static class AuthOptionsValidation
{
    public static bool HasRequiredSettings(AuthOptions options)
    {
        return IsAbsoluteUri(options.Authority)
            && IsAbsoluteUri(options.Issuer)
            && IsAbsoluteUri(options.DiscoveryUrl)
            && IsAbsoluteUri(options.JwksUrl)
            && !string.IsNullOrWhiteSpace(options.Audience)
            && !string.IsNullOrWhiteSpace(options.WebClientId);
    }

    public static bool HasValidPkceClients(AuthOptions options)
    {
        if (options.PkceClients.Count == 0)
        {
            return false;
        }

        HashSet<string> uniqueClientIds = new(StringComparer.Ordinal);

        foreach (PkceClientOptions client in options.PkceClients)
        {
            if (string.IsNullOrWhiteSpace(client.ClientId) || !uniqueClientIds.Add(client.ClientId))
            {
                return false;
            }

            if (client.RedirectUris.Count == 0)
            {
                return false;
            }

            if (client.RedirectUris.Any(uri => !IsAbsoluteUri(uri)))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAbsoluteUri(string value) =>
        !string.IsNullOrWhiteSpace(value) && Uri.TryCreate(value, UriKind.Absolute, out _);
}
