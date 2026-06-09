using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Common.SharedKernel.Exceptions;
using Common.SharedKernel.Logging;
using Inventory.Api.Infrastructure.Persistence;
using Inventory.Api.Observability;
using Npgsql;

namespace Inventory.Api.Infrastructure;

[ExcludeFromCodeCoverage]
internal static class ServiceRegistration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddInventoryApi(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("inventory")
                                   ?? configuration.GetConnectionString("inventorydb")
                                   ?? throw new ConfigurationException("Connection string 'inventory' or 'inventorydb'");

            services.AddValidationBehaviour();
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton(NpgsqlDataSource.Create(connectionString));
            services.AddSingleton<IInventoryStore, InventoryStore>();
            services.AddHostedService<InventorySchemaInitializer>();

            IConfigurationSection loggingSection = configuration.GetSection("Logging:CommonSharedKernel");
            var loggingServiceName = loggingSection["ServiceName"] ?? "inventory-api";
            var hasMinimumLevel = Enum.TryParse(
                loggingSection["MinimumLevel"],
                true,
                out Common.SharedKernel.Logging.LogLevel configuredMinimumLevel);
            Common.SharedKernel.Logging.LogLevel minimumLevel = hasMinimumLevel
                ? configuredMinimumLevel
                : Common.SharedKernel.Logging.LogLevel.Trace;
            var enabledLogTypes = loggingSection.GetSection("EnabledLogTypes").Get<string[]>()
                                  ?? ["api", "trace", "event"];

            var hasConsoleFormatter = Enum.TryParse(
                loggingSection["Console:FormatterKind"],
                true,
                out LogFormatterKind configuredConsoleFormatter);
            LogFormatterKind consoleFormatter = hasConsoleFormatter
                ? configuredConsoleFormatter
                : LogFormatterKind.Json;

            IConfigurationSection openSearchSection = loggingSection.GetSection("OpenSearch");
            var openSearchEnabled = bool.TryParse(openSearchSection["Enabled"], out var enabled) && enabled;
            var hasOpenSearchEndpoint = Uri.TryCreate(openSearchSection["Endpoint"], UriKind.Absolute, out Uri? openSearchEndpoint);
            var openSearchApiIndexPrefix = string.IsNullOrWhiteSpace(openSearchSection["ApiIndexPrefix"])
                ? "api-logs"
                : openSearchSection["ApiIndexPrefix"]!;
            var openSearchPayloadIndexPrefix = string.IsNullOrWhiteSpace(openSearchSection["PayloadIndexPrefix"])
                ? "api-payload"
                : openSearchSection["PayloadIndexPrefix"]!;
            var openSearchInfraIndexPrefix = string.IsNullOrWhiteSpace(openSearchSection["InfraIndexPrefix"])
                ? "infra-logs"
                : openSearchSection["InfraIndexPrefix"]!;
            var openSearchMessagingIndexPrefix = string.IsNullOrWhiteSpace(openSearchSection["MessagingIndexPrefix"])
                ? "messaging-log"
                : openSearchSection["MessagingIndexPrefix"]!;
            var useDailyIndexes = !bool.TryParse(openSearchSection["UseDailyIndexes"], out var configuredUseDailyIndexes)
                                  || configuredUseDailyIndexes;

            IConfigurationSection logStoreSection = loggingSection.GetSection("LogStore");
            var logStoreEnabled = bool.TryParse(logStoreSection["Enabled"], out var configuredLogStoreEnabled) && configuredLogStoreEnabled;
            var hasLogStoreEndpoint = Uri.TryCreate(logStoreSection["Endpoint"], UriKind.Absolute, out Uri? logStoreEndpoint);
            var logStoreCreateRoutePath = string.IsNullOrWhiteSpace(logStoreSection["CreateRoutePath"])
                ? "/api/v1/logs"
                : logStoreSection["CreateRoutePath"]!;

            services.AddCommonSharedKernelLogging(builder =>
            {
                builder.SetServiceName(loggingServiceName);
                builder.SetMinimumLevel(minimumLevel);
                builder.SetEnabledLogTypes(enabledLogTypes);
                builder.UseConsole(opts => opts.FormatterKind = consoleFormatter);

                if (openSearchEnabled && hasOpenSearchEndpoint && openSearchEndpoint is not null)
                {
                    builder.UseElasticsearch(opts =>
                    {
                        opts.Endpoint = openSearchEndpoint;
                        opts.ApiIndexPrefix = openSearchApiIndexPrefix;
                        opts.PayloadIndexPrefix = openSearchPayloadIndexPrefix;
                        opts.InfraIndexPrefix = openSearchInfraIndexPrefix;
                        opts.MessagingIndexPrefix = openSearchMessagingIndexPrefix;
                        opts.UseDailyIndexes = useDailyIndexes;
                    });
                }

                if (logStoreEnabled && hasLogStoreEndpoint && logStoreEndpoint is not null)
                {
                    builder.UseLogStore(opts =>
                    {
                        opts.Endpoint = logStoreEndpoint;
                        opts.CreateRoutePath = logStoreCreateRoutePath;
                    });
                }
            });

            services.UseRequestLoggingMiddleware<InventoryRequestLoggingMiddleware>();

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });

            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails();

            return services;
        }

        public IServiceCollection AddValidationBehaviour()
        {
            services.AddValidatorsFromAssemblyContaining<Program>();
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Common.SharedKernel.Validation.ValidationBehavior<,>));
            return services;
        }
    }
}
