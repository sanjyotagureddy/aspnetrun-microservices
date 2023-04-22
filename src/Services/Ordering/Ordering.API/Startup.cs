using EventBus.Messages.Common;
using MassTransit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Ordering.API.EventBusConsumers;
using Ordering.Application;
using Ordering.Infrastructure;

namespace Ordering.API;

public class Startup
{
  public Startup(IConfiguration configuration)
  {
    Configuration = configuration;
  }

  public IConfiguration Configuration { get; }

  // This method gets called by the runtime. Use this method to add services to the container.
  public void ConfigureServices(IServiceCollection services)
  {
    // combined services
    services.AddApplicationServices();
    services.AddInfrastructureServices(Configuration);

    // General Configurations
    services.AddAutoMapper(typeof(Startup));
    services.AddScoped<BasketCheckoutConsumer>();

    // MassTransit-RabbitMQ Configuration
    services.AddMassTransit(config =>
    {
      config.AddConsumer<BasketCheckoutConsumer>();
      config.UsingRabbitMq((ctx, cfg) =>
      {
        cfg.Host(Configuration["EventBusSettings:HostAddress"]);
        cfg.ReceiveEndpoint(EventBusConstants.BasketCheckoutQueue,
          c => { c.ConfigureConsumer<BasketCheckoutConsumer>(ctx); });
      });
    });
    services.AddMassTransitHostedService();

    services.AddControllers();
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

    app.UseRouting();

    app.UseAuthorization();

    app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
  }
}