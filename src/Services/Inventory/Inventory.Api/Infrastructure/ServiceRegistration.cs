using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Common.SharedKernel.Exceptions;
using Common.SharedKernel.Logging;
using Inventory.Api.Infrastructure.Persistence;
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

            services.AddCommonSharedKernelLogging(builder =>
            {
                builder.SetServiceName("Inventory.Api");
                builder.SetMinimumLevel(Common.SharedKernel.Logging.LogLevel.Trace);
                builder.UseConsole(opts => opts.FormatterKind = LogFormatterKind.Json);
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
