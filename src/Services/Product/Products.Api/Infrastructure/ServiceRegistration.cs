using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Common.SharedKernel;
using Common.SharedKernel.Exceptions;
using Common.SharedKernel.Logging;
using Common.SharedKernel.Messaging;

using Npgsql;

using Products.Api.Features.Products.Events;
using Products.Api.Infrastructure.Persistence;
using Products.Api.Observability;

namespace Products.Api.Infrastructure;

[ExcludeFromCodeCoverage]
internal static class ServiceRegistration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddProductsApi(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("productsdb")
                                   ?? configuration.GetConnectionString("products")
                                   ?? throw new ConfigurationException("Connection string 'productsdb' or 'products'");

            services.AddValidationBehaviour();
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton(NpgsqlDataSource.Create(connectionString));
            services.AddHostedService<ProductCatalogSchemaInitializer>();

            IConfigurationSection loggingSection = configuration.GetSection("Logging:CommonSharedKernel");
            string loggingServiceName = loggingSection["ServiceName"] ?? "products-api";
            bool hasMinimumLevel = Enum.TryParse(
                loggingSection["MinimumLevel"],
                true,
                out Common.SharedKernel.Logging.LogLevel configuredMinimumLevel);
            Common.SharedKernel.Logging.LogLevel minimumLevel = hasMinimumLevel
                ? configuredMinimumLevel
                : Common.SharedKernel.Logging.LogLevel.Trace;
            string[] enabledLogTypes = loggingSection.GetSection("EnabledLogTypes").Get<string[]>()
                ?? ["api", "trace", "event"];

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
            string openSearchApiIndexPrefix = string.IsNullOrWhiteSpace(openSearchSection["ApiIndexPrefix"])
                ? "api-logs"
                : openSearchSection["ApiIndexPrefix"]!;
            string openSearchInfraIndexPrefix = string.IsNullOrWhiteSpace(openSearchSection["InfraIndexPrefix"])
                ? "infra-logs"
                : openSearchSection["InfraIndexPrefix"]!;
            string openSearchMessagingIndexPrefix = string.IsNullOrWhiteSpace(openSearchSection["MessagingIndexPrefix"])
                ? "messaging-log"
                : openSearchSection["MessagingIndexPrefix"]!;
            bool useDailyIndexes = !bool.TryParse(openSearchSection["UseDailyIndexes"], out bool configuredUseDailyIndexes)
                || configuredUseDailyIndexes;

            IConfigurationSection logStoreSection = loggingSection.GetSection("LogStore");
            bool logStoreEnabled = bool.TryParse(logStoreSection["Enabled"], out bool configuredLogStoreEnabled) && configuredLogStoreEnabled;
            bool hasLogStoreEndpoint = Uri.TryCreate(logStoreSection["Endpoint"], UriKind.Absolute, out Uri? logStoreEndpoint);
            string logStoreCreateRoutePath = string.IsNullOrWhiteSpace(logStoreSection["CreateRoutePath"])
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

            services.UseRequestLoggingMiddleware<ProductsRequestLoggingMiddleware>();

            services.AddMessaging(builder =>
            {
                builder.Options.TopicPrefix = configuration["Messaging:TopicPrefix"] ?? string.Empty;
                builder.Options.ProvisioningMode = ProvisioningMode.ValidateOnly;

                builder.RegisterDestination(destination =>
                {
                    destination.DestinationName = ProductCreatedIntegrationEvent.Topic;
                    destination.OwnerService = "products-api";
                    destination.Contract = new MessageContractDescriptor("ProductLifecycleEvent", "1.0", "application/json");
                    destination.PartitioningStrategy = PartitioningStrategy.ByAggregateId;
                    destination.PartitionKeySelector = "payload.productId";
                });

                builder.UseKafka(options =>
                {
                    options.BootstrapServers = configuration.GetConnectionString("message-broker")
                                               ?? configuration["Messaging:Kafka:BootstrapServers"]
                                               ?? "localhost:9092";
                    options.ConsumerGroup = configuration["Messaging:Kafka:ConsumerGroup"] ?? "products-api";
                });
            });

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });


            services.AddSingleton<IProductCatalogStore, ProductCatalogStore>();
            services.AddHttpClient<IInventoryStockAdapter, InventoryStockAdapter>((provider, client) =>
                {
                    IConfiguration config = provider.GetRequiredService<IConfiguration>();
                    var configuredBaseUrl = config["Services:Inventory:BaseUrl"];
                    var resolvedBaseUrl = string.IsNullOrWhiteSpace(configuredBaseUrl)
                        ? "https+http://inventory-api"
                        : configuredBaseUrl;

                    client.BaseAddress = new Uri(resolvedBaseUrl, UriKind.Absolute);
                    client.Timeout = new TimeSpan(0, 0, 60);
                    client.DefaultRequestHeaders.Add(Constants.Headers.CallerService, "products-api");
                })
                .AddServiceDiscovery()
                .AddStandardResilienceHandler();

            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails();

            return services;
        }

        private void AddValidationBehaviour()
        {
            services.AddValidatorsFromAssemblyContaining<Program>();
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Common.SharedKernel.Validation.ValidationBehavior<,>));
        }
    }
}
