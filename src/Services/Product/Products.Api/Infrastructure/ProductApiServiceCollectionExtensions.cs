using System.Reflection;
using Npgsql;

namespace Products.Api.Infrastructure;

internal static class ProductApiServiceCollectionExtensions
{
    public static IServiceCollection AddProductsApi(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("productsdb")
                               ?? throw new InvalidOperationException("Connection string 'productsdb' was not found.");

        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(NpgsqlDataSource.Create(connectionString));
        services.AddHostedService<ProductCatalogSchemaInitializer>();

       services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        services.AddValidatorsFromAssemblyContaining<Program>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddSingleton<IProductCatalogStore, ProductCatalogStore>();

        services.AddExceptionHandler<ProductApiExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }
}
