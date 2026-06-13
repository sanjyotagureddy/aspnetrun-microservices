using Microsoft.Extensions.Configuration;

namespace Common.SharedKernel.Logging;

public static class LoggingConfigurationExtensions
{
    public static IServiceCollection AddConfiguredCommonSharedKernelLogging(
        this IServiceCollection services,
        IConfiguration configuration,
        string defaultServiceName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultServiceName);

        CommonSharedKernelLoggingConfiguration configured = configuration
            .GetSection("Logging:CommonSharedKernel")
            .Get<CommonSharedKernelLoggingConfiguration>()
            ?? new CommonSharedKernelLoggingConfiguration();

        IConfigurationSection loggingSection = configuration.GetSection("Logging:CommonSharedKernel");

        string serviceName = string.IsNullOrWhiteSpace(configured.ServiceName)
            ? defaultServiceName
            : configured.ServiceName;

        LogLevel minimumLevel = configured.MinimumLevel ?? LogLevel.Trace;

        string[] enabledLogTypes = configured.EnabledLogTypes?.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray()
            ?? ["api", "trace", "error"];

        LogFormatterKind formatterKind = Enum.TryParse(loggingSection["Console:FormatterKind"], true, out LogFormatterKind configuredFormatter)
            ? configuredFormatter
            : LogFormatterKind.Json;

        IConfigurationSection openSearch = loggingSection.GetSection("OpenSearch");
        bool openSearchEnabled = bool.TryParse(openSearch["Enabled"], out bool configuredOpenSearchEnabled)
            && configuredOpenSearchEnabled;

        string openSearchEndpoint = openSearch["Endpoint"] ?? "http://localhost:9200";
        string appIndexPrefix = string.IsNullOrWhiteSpace(openSearch["AppIndexPrefix"])
            ? "app-log"
            : openSearch["AppIndexPrefix"]!;
        string messagingIndexPrefix = string.IsNullOrWhiteSpace(openSearch["MessagingIndexPrefix"])
            ? "messaging-log"
            : openSearch["MessagingIndexPrefix"]!;
        string auditIndexPrefix = string.IsNullOrWhiteSpace(openSearch["AuditIndexPrefix"])
            ? "audit-log"
            : openSearch["AuditIndexPrefix"]!;
        string securityIndexPrefix = string.IsNullOrWhiteSpace(openSearch["SecurityIndexPrefix"])
            ? "security-log"
            : openSearch["SecurityIndexPrefix"]!;
        string payloadIndexPrefix = string.IsNullOrWhiteSpace(openSearch["PayloadIndexPrefix"])
            ? "payload-log"
            : openSearch["PayloadIndexPrefix"]!;
        bool useDailyIndexes = !bool.TryParse(openSearch["UseDailyIndexes"], out bool configuredUseDailyIndexes)
            || configuredUseDailyIndexes;

        bool hasEndpoint = Uri.TryCreate(openSearchEndpoint, UriKind.Absolute, out Uri? endpoint);

        services.AddCommonSharedKernelLogging(builder =>
        {
            builder.SetServiceName(serviceName);
            builder.SetMinimumLevel(minimumLevel);
            builder.SetEnabledLogTypes(enabledLogTypes);
            builder.UseConsole(options => options.FormatterKind = formatterKind);

            if (openSearchEnabled && hasEndpoint && endpoint is not null)
            {
                builder.UseElasticsearch(options =>
                {
                    options.Endpoint = endpoint;
                    options.AppIndexPrefix = appIndexPrefix;
                    options.MessagingIndexPrefix = messagingIndexPrefix;
                    options.AuditIndexPrefix = auditIndexPrefix;
                    options.SecurityIndexPrefix = securityIndexPrefix;
                    options.PayloadIndexPrefix = payloadIndexPrefix;
                    options.UseDailyIndexes = useDailyIndexes;
                });
            }
        });

        return services;
    }

    private sealed class CommonSharedKernelLoggingConfiguration
    {
        public string ServiceName { get; set; } = string.Empty;

        public LogLevel? MinimumLevel { get; set; }

        public string[]? EnabledLogTypes { get; set; }
    }
}
