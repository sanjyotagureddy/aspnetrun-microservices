using Common.SharedKernel.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Common.SharedKernel.Messaging;

internal sealed class MessagingConfigurationValidationHostedService(
    IOptions<MessagingOptions> options,
    IHostEnvironment hostEnvironment,
    IMessagingProvider provider,
    ILogger<MessagingConfigurationValidationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<string> errors = Validate(options.Value);
        if (errors.Count == 0)
        {
            await logger.LogEventAsync(
                new TraceLog
                {
                    Message = "Messaging configuration validated successfully.",
                    Category = "messaging.configuration.validate",
                    Context = new Dictionary<string, object?>
                    {
                        ["destinations"] = options.Value.Destinations.Count,
                        ["provider"] = provider.Name,
                        ["supportsPartitioning"] = provider.Capabilities.SupportsPartitioning,
                        ["supportsOrderingByKey"] = provider.Capabilities.SupportsOrderingByKey,
                        ["supportsTransactions"] = provider.Capabilities.SupportsTransactions
                    }
                },
                cancellationToken);

            try
            {
                await provider
                    .CreateProvisioner()
                    .EnsureAsync(options.Value.Destinations.AsReadOnly(), options.Value.ProvisioningMode, cancellationToken);
            }
            catch (Exception ex) when (hostEnvironment.IsDevelopment())
            {
                await logger.LogEventAsync(
                    new TraceLog
                    {
                        Message = "Messaging destination provisioning warning in development environment.",
                        Category = "messaging.configuration.provision",
                        Context = new Dictionary<string, object?>
                        {
                            ["error"] = ex.Message,
                            ["mode"] = options.Value.ProvisioningMode.ToString(),
                            ["provider"] = provider.Name
                        }
                    },
                    cancellationToken);

                return;
            }

            await logger.LogEventAsync(
                new TraceLog
                {
                    Message = "Messaging destination provisioning completed.",
                    Category = "messaging.configuration.provision",
                    Context = new Dictionary<string, object?>
                    {
                        ["destinations"] = options.Value.Destinations.Count,
                        ["mode"] = options.Value.ProvisioningMode.ToString(),
                        ["provider"] = provider.Name
                    }
                },
                cancellationToken);

            return;
        }

        string message = string.Join(Environment.NewLine, errors.Select((error, index) => $"{index + 1}. {error}"));
        if (hostEnvironment.IsDevelopment())
        {
            await logger.LogEventAsync(
                new TraceLog
                {
                    Message = "Messaging configuration validation warnings.",
                    Category = "messaging.configuration.validate",
                    Context = new Dictionary<string, object?>
                    {
                        ["errors"] = message
                    }
                },
                cancellationToken);
            return;
        }

        throw new MessagingConfigurationException($"Messaging configuration is invalid:{Environment.NewLine}{message}");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static IReadOnlyList<string> Validate(MessagingOptions options)
    {
        List<string> errors = [];

        if (options.Provider == MessagingProviderKind.None)
        {
            errors.Add("Messaging provider is not configured.");
        }

        for (int i = 0; i < options.Destinations.Count; i++)
        {
            DestinationRegistration destination = options.Destinations[i];
            string label = $"Destination[{i}]";

            if (string.IsNullOrWhiteSpace(destination.DestinationName))
            {
                errors.Add($"{label}: DestinationName is required.");
            }

            if (string.IsNullOrWhiteSpace(destination.OwnerService))
            {
                errors.Add($"{label}: OwnerService is required.");
            }

            if (string.IsNullOrWhiteSpace(destination.Contract.MessageType))
            {
                errors.Add($"{label}: Contract.MessageType is required.");
            }

            if (string.IsNullOrWhiteSpace(destination.Contract.Version))
            {
                errors.Add($"{label}: Contract.Version is required.");
            }
            else if (!IsVersionFormatSupported(destination.Contract.Version))
            {
                errors.Add($"{label}: Contract.Version '{destination.Contract.Version}' must be '<major>' or '<major>.<minor>'.");
            }

            bool requiresKey = destination.PartitioningStrategy is PartitioningStrategy.ByAggregateId or PartitioningStrategy.ByOrderingKey or PartitioningStrategy.ByRoutingKey;
            if (requiresKey && string.IsNullOrWhiteSpace(destination.PartitionKeySelector))
            {
                errors.Add($"{label}: PartitionKeySelector is required for {destination.PartitioningStrategy}.");
            }

            if (destination.OrderingRequired && destination.PartitioningStrategy == PartitioningStrategy.None)
            {
                errors.Add($"{label}: OrderingRequired cannot be true when PartitioningStrategy is None.");
            }

            if (!string.IsNullOrWhiteSpace(destination.PartitionKeySelector) && IsEventIdSelector(destination.PartitionKeySelector))
            {
                errors.Add($"{label}: PartitionKeySelector cannot be event-id based for ordered streams.");
            }
        }

        var duplicates = options.Destinations
            .GroupBy(destination => destination.DestinationName, StringComparer.OrdinalIgnoreCase)
            .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        errors.AddRange(duplicates.Select(duplicate => $"Duplicate destination registration detected for '{duplicate}'."));

        return errors;
    }

    private static bool IsEventIdSelector(string selector)
    {
        string normalized = selector.Replace("_", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("-", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(".", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(" ", string.Empty, StringComparison.OrdinalIgnoreCase)
            .ToLowerInvariant();

        return normalized.Contains("eventid", StringComparison.Ordinal);
    }

    private static bool IsVersionFormatSupported(string version)
    {
        var parts = version.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length is < 1 or > 2)
        {
            return false;
        }

        return parts.All(part => int.TryParse(part, out _));
    }
}
