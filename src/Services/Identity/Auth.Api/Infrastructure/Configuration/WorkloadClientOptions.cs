using System.Diagnostics.CodeAnalysis;

namespace Auth.Api.Infrastructure.Configuration;

internal sealed class WorkloadClientOptions
{
    public string ClientId { get; set; } = string.Empty;
    public List<string> AllowedScopes { get; set; } = [];
}

internal sealed class WorkloadAuthOptions
{
    public List<WorkloadClientOptions> Clients { get; set; } = [];
}

[ExcludeFromCodeCoverage]
internal static class WorkloadAuthOptionsValidation
{
    public static bool HasValidClients(WorkloadAuthOptions options)
    {
        if (options.Clients.Count == 0)
        {
            return false;
        }

        HashSet<string> uniqueClientIds = new(StringComparer.Ordinal);

        foreach (WorkloadClientOptions client in options.Clients)
        {
            if (string.IsNullOrWhiteSpace(client.ClientId) || !uniqueClientIds.Add(client.ClientId))
            {
                return false;
            }

            if (client.AllowedScopes.Count == 0)
            {
                return false;
            }

            if (client.AllowedScopes.Any(string.IsNullOrWhiteSpace))
            {
                return false;
            }
        }

        return true;
    }
}
