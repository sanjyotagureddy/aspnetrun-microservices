using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Common.SharedKernel.Exceptions;
using Common.SharedKernel.Logging;
using Common.SharedKernel.Messaging;
using Inventory.Api.Features.Inventory.Events;
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
            services.AddInventoryCoreInfrastructure(connectionString);
            services.AddInventoryObservabilityAndMessaging(configuration);
            services.AddInventoryApplicationLayer();
            services.AddInventoryExceptionHandling();

            return services;
        }

        private void AddInventoryCoreInfrastructure(string connectionString)
        {
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton(NpgsqlDataSource.Create(connectionString));
            services.AddSingleton<IInventoryStore, InventoryStore>();
            services.AddSingleton<IInventoryTransactionExecutor, InventoryTransactionExecutor>();
            services.AddSingleton<IInventoryOutboxStore, InventoryOutboxStore>();
            services.AddSingleton<IInventoryDomainEventDispatcher, InventoryDomainEventDispatcher>();
            services.AddHostedService<InventorySchemaInitializer>();
            services.AddHostedService<InventoryOutboxPublisher>();
        }

        private void AddInventoryObservabilityAndMessaging(IConfiguration configuration)
        {
            services.AddConfiguredCommonSharedKernelLogging(configuration, "inventory-api");
            services.UseRequestLoggingMiddleware<InventoryRequestLoggingMiddleware>();

            services.AddMessaging(builder =>
            {
                builder.ApplyConfiguration(configuration, "inventory-api");
                ConfigureInventoryDestinations(builder);
            });
        }

        private static void ConfigureInventoryDestinations(IMessagingBuilder builder)
        {
            builder.RegisterDestination(destination =>
            {
                destination.DestinationName = InventoryInitializedIntegrationEvent.Topic;
                destination.OwnerService = "inventory-api";
                destination.Contract = new MessageContractDescriptor(InventoryInitializedIntegrationEvent.EventTypeName, "1.0", "application/json");
                destination.PartitioningStrategy = PartitioningStrategy.ByAggregateId;
                destination.PartitionKeySelector = "payload.productId";
                destination.OrderingRequired = true;
            });
        }

        private void AddInventoryApplicationLayer()
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });
        }

        private void AddInventoryExceptionHandling()
        {
            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails();
        }

        public IServiceCollection AddValidationBehaviour()
        {
            services.AddValidatorsFromAssemblyContaining<Program>();
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Common.SharedKernel.Validation.ValidationBehavior<,>));
            return services;
        }
    }
}
