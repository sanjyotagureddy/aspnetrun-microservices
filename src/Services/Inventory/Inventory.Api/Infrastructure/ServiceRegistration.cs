using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Common.SharedKernel.Exceptions;
using Common.SharedKernel.Logging;
using Common.SharedKernel.Messaging;
using Inventory.Api.Features.Inventory.Events;
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
            services.AddSingleton<IInventoryOutboxStore, InventoryOutboxStore>();
            services.AddSingleton<IInventoryDomainEventDispatcher, InventoryDomainEventDispatcher>();
            services.AddHostedService<InventorySchemaInitializer>();
            services.AddHostedService<InventoryOutboxPublisher>();

            IConfigurationSection loggingSection = configuration.GetSection("Logging:CommonSharedKernel");
            string loggingServiceName = loggingSection["ServiceName"] ?? "inventory-api";
            bool hasMinimumLevel = Enum.TryParse(
                loggingSection["MinimumLevel"],
                true,
                out Common.SharedKernel.Logging.LogLevel configuredMinimumLevel);
            Common.SharedKernel.Logging.LogLevel minimumLevel = hasMinimumLevel
                ? configuredMinimumLevel
                : Common.SharedKernel.Logging.LogLevel.Trace;
            string[] enabledLogTypes = loggingSection.GetSection("EnabledLogTypes").Get<string[]>()
                ?? ["api", "trace", "error"];

            bool hasConsoleFormatter = Enum.TryParse(
                loggingSection["Console:FormatterKind"],
                true,
                out LogFormatterKind configuredConsoleFormatter);
            LogFormatterKind consoleFormatter = hasConsoleFormatter
                ? configuredConsoleFormatter
                : LogFormatterKind.Json;

            IConfigurationSection openSearchSection = loggingSection.GetSection("OpenSearch");
            bool openSearchEnabled = bool.TryParse(openSearchSection["Enabled"], out bool enabled) && enabled;
            bool hasOpenSearchEndpoint = Uri.TryCreate(openSearchSection["Endpoint"], UriKind.Absolute, out Uri? openSearchEndpoint);
            string openSearchAppIndexPrefix = string.IsNullOrWhiteSpace(openSearchSection["AppIndexPrefix"])
                ? "app-log"
                : openSearchSection["AppIndexPrefix"]!;
            string openSearchMessagingIndexPrefix = string.IsNullOrWhiteSpace(openSearchSection["MessagingIndexPrefix"])
                ? "messaging-log"
                : openSearchSection["MessagingIndexPrefix"]!;
            string openSearchAuditIndexPrefix = string.IsNullOrWhiteSpace(openSearchSection["AuditIndexPrefix"])
                ? "audit-log"
                : openSearchSection["AuditIndexPrefix"]!;
            string openSearchSecurityIndexPrefix = string.IsNullOrWhiteSpace(openSearchSection["SecurityIndexPrefix"])
                ? "security-log"
                : openSearchSection["SecurityIndexPrefix"]!;
            string openSearchPayloadIndexPrefix = string.IsNullOrWhiteSpace(openSearchSection["PayloadIndexPrefix"])
                ? "payload-log"
                : openSearchSection["PayloadIndexPrefix"]!;
            bool useDailyIndexes = !bool.TryParse(openSearchSection["UseDailyIndexes"], out bool configuredUseDailyIndexes)
                || configuredUseDailyIndexes;

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
                        opts.AppIndexPrefix = openSearchAppIndexPrefix;
                        opts.MessagingIndexPrefix = openSearchMessagingIndexPrefix;
                        opts.AuditIndexPrefix = openSearchAuditIndexPrefix;
                        opts.SecurityIndexPrefix = openSearchSecurityIndexPrefix;
                        opts.PayloadIndexPrefix = openSearchPayloadIndexPrefix;
                        opts.UseDailyIndexes = useDailyIndexes;
                    });
                }
            });

            services.UseRequestLoggingMiddleware<InventoryRequestLoggingMiddleware>();

            services.AddMessaging(builder =>
            {
                builder.Options.TopicPrefix = configuration["Messaging:TopicPrefix"] ?? string.Empty;
                builder.Options.ProvisioningMode = ProvisioningMode.ValidateOnly;

                builder.RegisterDestination(destination =>
                {
                    destination.DestinationName = InventoryInitializedIntegrationEvent.Topic;
                    destination.OwnerService = "inventory-api";
                    destination.Contract = new MessageContractDescriptor(InventoryInitializedIntegrationEvent.EventTypeName, "1.0", "application/json");
                    destination.PartitioningStrategy = PartitioningStrategy.ByAggregateId;
                    destination.PartitionKeySelector = "payload.productId";
                });

                builder.UseKafka(options =>
                {
                    options.BootstrapServers = configuration.GetConnectionString("message-broker")
                                               ?? configuration["Messaging:Kafka:BootstrapServers"]
                                               ?? "localhost:9092";
                    options.ConsumerGroup = configuration["Messaging:Kafka:ConsumerGroup"] ?? "inventory-api";
                });
            });

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
