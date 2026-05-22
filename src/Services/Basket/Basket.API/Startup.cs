using Basket.API.GrpcServices;
using Basket.API.Repositories;
using Basket.API.Repositories.Interfaces;
using Discount.Grpc.Protos;
using MassTransit;
using Microsoft.OpenApi;
using SharedKernel;
using SharedKernel.Middleware;

namespace Basket.API;

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

        // General Configuration
        services.AddScoped<IBasketRepository, BasketRepository>();
        services.AddAutoMapper(cfg => { }, typeof(Startup));

        // Grpc Configuration
        services.AddGrpcClient<DiscountProtoService.DiscountProtoServiceClient>
          (o => o.Address = new Uri(Configuration["GrpcSettings:DiscountUrl"]));
        services.AddScoped<DiscountGrpcService>();

        // MassTransit-RabbitMQ Configuration
        services.AddMassTransit(config =>
        {
            config.UsingRabbitMq((ctx, cfg) => { cfg.Host(Configuration["EventBusSettings:HostAddress"]); });
        });

        services.AddControllers();
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
            endpoints.MapControllers();
        });
    }
}
