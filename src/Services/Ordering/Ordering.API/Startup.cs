using MassTransit;
using Microsoft.OpenApi;
using Ordering.API.EventBusConsumers;
using Ordering.Application;
using Ordering.Infrastructure;
using SharedKernel;
using SharedKernel.Middleware;

namespace Ordering.API;

public class Startup(IConfiguration configuration)
{
    public IConfiguration Configuration { get; } = configuration;

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        // combined services
        services.AddApplicationServices();
        services.AddInfrastructureServices(Configuration);

        services.AddAutoMapper(cfg => { }, typeof(Startup));
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<Startup>());
        services.AddScoped<BasketCheckoutConsumer>();

        // MassTransit-RabbitMQ Configuration
        services.AddMassTransit(config =>
        {
            config.AddConsumer<BasketCheckoutConsumer>();
            config.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.Host(Configuration["EventBusSettings:HostAddress"]);
            cfg.ReceiveEndpoint(Constants.EventBusConstants.BasketCheckoutQueue,
          c => { c.ConfigureConsumer<BasketCheckoutConsumer>(ctx); });
        });
        });

        services.AddControllers();
        services.AddHealthChecks();
        services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = "Ordering.API", Version = "v1" }); });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Ordering.API v1"));
        }

        app.UseMiddleware<RequestContextMiddleware>();
        // Global exception handler for the service (returns SharedKernel.Errors.Error payload)
        app.UseGlobalExceptionHandler(Constants.ServiceCodes.Ordering);
        app.UseRouting();

        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapHealthChecks("/health");
            endpoints.MapControllers();
        });
    }
}
