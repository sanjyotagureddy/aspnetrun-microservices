using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Common.SharedKernel;
using Common.SharedKernel.Exceptions;
using Common.SharedKernel.Logging;
using Common.SharedKernel.Messaging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Npgsql;
using Products.Api.Features.Products.Events;
using Products.Api.Observability;
using Products.Api.Infrastructure.Security;

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
            services.AddProductsAuthenticationAndAuthorization(configuration);
            services.AddProductsCoreInfrastructure(connectionString);
            services.AddProductsObservabilityAndMessaging(configuration);
            services.AddProductsApplicationLayer();
            services.AddProductsInfrastructureAdapters();
            services.AddProductsExceptionHandling();

            return services;
        }

        private void AddProductsCoreInfrastructure(string connectionString)
        {
            services.AddSingleton(TimeProvider.System);
            services.AddSingleton(NpgsqlDataSource.Create(connectionString));
            services.AddHostedService<ProductCatalogSchemaInitializer>();
        }

        private void AddProductsAuthenticationAndAuthorization(IConfiguration configuration)
        {
            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure(options =>
                {
                    options.Authority = configuration["Auth:Authority"];
                    options.Audience = configuration["Auth:Audience"] ?? "products-api";
                    options.RequireHttpsMetadata = false;
                    options.TokenValidationParameters.ValidIssuer = configuration["Auth:Issuer"];
                    options.TokenValidationParameters.ValidAudience = configuration["Auth:Audience"] ?? "products-api";
                });

            services.AddAuthorizationBuilder()
                .AddPolicy(ProductPolicyNames.UserPrincipalOnly, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("sub");
                    policy.RequireClaim("tenant_id");
                })
                .AddPolicy(ProductPolicyNames.TenantReadPolicy, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("sub");
                    policy.RequireClaim("tenant_id");
                    policy.RequireAssertion(context =>
                    {
                        ClaimsPrincipal user = context.User;
                        bool hasReadScope = user.FindAll("scope")
                            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                            .Any(scope => string.Equals(scope, "products.read", StringComparison.Ordinal));

                        bool hasRole = user.FindAll("role")
                            .Concat(user.FindAll(ClaimTypes.Role))
                            .Select(claim => claim.Value)
                            .Any(role => role is ProductRoleNames.TenantAdmin or ProductRoleNames.CatalogManager or ProductRoleNames.Buyer or ProductRoleNames.PlatformAdmin);

                        return hasReadScope && hasRole;
                    });
                })
                .AddPolicy(ProductPolicyNames.CatalogWritePolicy, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("sub");
                    policy.RequireClaim("tenant_id");
                    policy.RequireAssertion(context =>
                    {
                        ClaimsPrincipal user = context.User;

                        bool hasWriteScope = user.FindAll("scope")
                            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                            .Any(scope => string.Equals(scope, "products.write", StringComparison.Ordinal));

                        bool hasWriteRole = user.FindAll("role")
                            .Concat(user.FindAll(ClaimTypes.Role))
                            .Select(claim => claim.Value)
                            .Any(role => role is ProductRoleNames.TenantAdmin or ProductRoleNames.CatalogManager);

                        return hasWriteScope && hasWriteRole;
                    });
                });
        }

        private void AddProductsObservabilityAndMessaging(IConfiguration configuration)
        {
            services.AddConfiguredCommonSharedKernelLogging(configuration, "products-api");
            services.UseRequestLoggingMiddleware<ProductsRequestLoggingMiddleware>();

            services.AddMessaging(builder =>
            {
                builder.ApplyConfiguration(configuration, "products-api");
                ConfigureProductDestinations(builder);
            });
        }

        private static void ConfigureProductDestinations(IMessagingBuilder builder)
        {
            builder.RegisterDestination(destination =>
            {
                destination.DestinationName = ProductCreatedIntegrationEvent.Topic;
                destination.OwnerService = "products-api";
                destination.Contract = new MessageContractDescriptor("ProductLifecycleEvent", "1.0", "application/json");
                destination.PartitioningStrategy = PartitioningStrategy.ByAggregateId;
                destination.PartitionKeySelector = "payload.productId";
                destination.OrderingRequired = true;
            });
        }

        private void AddProductsApplicationLayer()
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });
        }

        private void AddProductsInfrastructureAdapters()
        {
            services.AddSingleton<IProductCatalogStore, ProductCatalogStore>();
            services.AddSingleton<IProductTransactionExecutor, ProductTransactionExecutor>();
            services.AddSingleton<IProductOutboxStore, ProductOutboxStore>();
            services.AddSingleton<IProductDomainEventDispatcher, ProductDomainEventDispatcher>();
            services.AddHostedService<ProductOutboxPublisher>();
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
        }

        private void AddProductsExceptionHandling()
        {
            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails();
        }

        private void AddValidationBehaviour()
        {
            services.AddValidatorsFromAssemblyContaining<Program>();
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Common.SharedKernel.Validation.ValidationBehavior<,>));
        }
    }
}
