using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Common.SharedKernel.Exceptions;
using Common.SharedKernel.Logging;
using Common.SharedKernel.Messaging;

using Npgsql;

using Products.Api.Infrastructure.Persistence;

namespace Products.Api.Infrastructure;

[ExcludeFromCodeCoverage]
internal static class ServiceRegistration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddProductsApi(IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("productsdb")
                                   ?? throw new ConfigurationException("Connection string");

            services.AddValidationBehaviour();
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton(NpgsqlDataSource.Create(connectionString));
            services.AddHostedService<ProductCatalogSchemaInitializer>();

            services.AddCommonSharedKernelLogging(builder =>
            {
                builder.SetServiceName("Products.Api");
                builder.SetMinimumLevel(Common.SharedKernel.Logging.LogLevel.Trace);
                builder.UseConsole(opts => opts.FormatterKind = LogFormatterKind.Json);
            });

            services.AddMessaging(builder =>
            {
                builder.Options.TopicPrefix = configuration["Messaging:TopicPrefix"] ?? string.Empty;
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
