using Catalog.API.Application.Behaviors;
using Catalog.API.Application.Contracts.Persistence;
using Catalog.API.Application.Features.Products.Commands.CreateProduct;
using Catalog.API.Application.Features.Products.Commands.UpdateProduct;
using Catalog.API.Application.Features.Products.Validators;
using Catalog.API.Infrastructure.Persistence;
using Catalog.API.Infrastructure.Persistence.Repositories;
using FluentValidation;
using MediatR;
using Microsoft.OpenApi;
using SharedKernel;
using SharedKernel.Errors;
using SharedKernel.Middleware;
using SharedKernel.Web;

namespace Catalog.API;

public class Startup(IConfiguration configuration)
{
    public IConfiguration Configuration { get; } = configuration;

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddControllers();
        services.AddHealthChecks();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Startup>());
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient<IValidator<CreateProductCommand>, CreateProductCommandValidator>();
        services.AddTransient<IValidator<UpdateProductCommand>, UpdateProductCommandValidator>();

        var useRedis = Configuration.GetValue<bool>("CacheSettings:UseRedis");
        var redisConnectionString = Configuration.GetValue<string>("CacheSettings:ConnectionString") ?? throw Errors.ServerSide.ConfigurationMissing("Missing Redis connection string.");
        if (useRedis && !string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options => { options.Configuration = redisConnectionString; });
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = "Catalog.API", Version = "v1" }); });

        services.AddScoped<ICatalogContext, CatalogContext>();
        services.AddScoped<IProductRepository, ProductRepository>();
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog.API v1"));
        }

        app.UseMiddleware<RequestContextMiddleware>();

        // Global exception handler for the service (returns SharedKernel.Errors.Error payload)
        app.UseGlobalExceptionHandler(Constants.ServiceCodes.Catalog);

        app.UseRouting();
        app.UseMiddleware<IdempotencyMiddleware>();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHealthChecks("/health");
            endpoints.MapDiscoveredEndpoints();
        });
    }
}
