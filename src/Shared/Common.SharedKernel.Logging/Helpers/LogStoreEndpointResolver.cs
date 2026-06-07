namespace Common.SharedKernel.Logging;

internal static class LogStoreEndpointResolver
{
    public static Uri Resolve(Uri configuredEndpoint)
    {
        Guard.Against.Null(configuredEndpoint);

        if (!configuredEndpoint.IsAbsoluteUri)
        {
            return configuredEndpoint;
        }

        if (IsLocalOrIp(configuredEndpoint.Host))
        {
            return configuredEndpoint;
        }

        string hostKey = configuredEndpoint.Host.Trim().ToLowerInvariant();
        string preferredScheme = configuredEndpoint.Scheme;

        string[] candidates = preferredScheme.Equals("https", StringComparison.OrdinalIgnoreCase)
            ?
            [
                $"services__{hostKey}__https__0",
                $"services__{hostKey}__http__0"
            ]
            :
            [
                $"services__{hostKey}__http__0",
                $"services__{hostKey}__https__0"
            ];

        foreach (string key in candidates)
        {
            string? value = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (Uri.TryCreate(value, UriKind.Absolute, out Uri? resolved))
            {
                return resolved;
            }
        }

        return configuredEndpoint;
    }

    private static bool IsLocalOrIp(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return false;
        }

        if (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return System.Net.IPAddress.TryParse(host, out _);
    }
}
