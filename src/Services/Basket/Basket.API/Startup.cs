using System.Diagnostics.CodeAnalysis;

using Basket.API.Application.Contracts.Infrastructure;
using Basket.API.Application.Contracts.Persistence;
using Basket.API.Infrastructure.Messaging;
using Basket.API.Infrastructure.Persistence;
using Basket.API.Infrastructure.Services;
using Discount.Grpc.Protos;
using MassTransit;
using Microsoft.OpenApi;
using SharedKernel;
using SharedKernel.Middleware;
using SharedKernel.Errors;
using SharedKernel.Web;

namespace Basket.API;

[ExcludeFromCodeCoverage]
public class Startup(IConfiguration configuration)
{
    public IConfiguration Configuration { get; } = configuration;

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        // Redis Configuration
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = Configuration.GetValue<string>("CacheSettings:ConnectionString");
        });

        // CQRS and application contracts
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Startup>());
        services.AddScoped<IBasketRepository, BasketRepository>();
        services.AddScoped<IDiscountService, DiscountGrpcService>();
        services.AddScoped<IBasketCheckoutPublisher, BasketCheckoutPublisher>();

        // Grpc Configuration
                services.AddGrpcClient<DiscountProtoService.DiscountProtoServiceClient>
                    (o => o.Address = new Uri(Configuration["GrpcSettings:DiscountUrl"] ?? throw Errors.ServerSide.ConfigurationMissing("Missing GrpcSettings:DiscountUrl")));

        // MassTransit-RabbitMQ Configuration
        services.AddMassTransit(config =>
        {
                        config.UsingRabbitMq((_, cfg) => { cfg.Host(Configuration["EventBusSettings:HostAddress"] ?? throw Errors.ServerSide.ConfigurationMissing("Missing EventBusSettings:HostAddress")); });
        });

        services.AddEndpointsApiExplorer();
        services.AddHealthChecks();
        services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = "Basket.API", Version = "v1" }); });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Basket.API v1"));
        }
        app.UseMiddleware<RequestContextMiddleware>();
        // Global exception handler for the service (returns SharedKernel.Errors.Error payload)
        app.UseGlobalExceptionHandler(Constants.ServiceCodes.Basket);


        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHealthChecks("/health");
            endpoints.MapDiscoveredEndpoints();
        });
    }
}
