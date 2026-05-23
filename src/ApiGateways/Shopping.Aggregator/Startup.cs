using Microsoft.OpenApi;
using SharedKernel;
using SharedKernel.Errors;
using SharedKernel.Middleware;
using Shopping.Aggregator.Services;
using Shopping.Aggregator.Services.Interfaces;

namespace Shopping.Aggregator;

public class Startup(IConfiguration configuration)
{
  public IConfiguration Configuration { get; } = configuration;

  // This method gets called by the runtime. Use this method to add services to the container.
  public void ConfigureServices(IServiceCollection services)
  {
    services.AddHttpClient<ICatalogService, CatalogService>(c =>
      c.BaseAddress = new Uri(Configuration["ApiSettings:CatalogUrl"] ?? throw Errors.ServerSide.ConfigurationMissing("Missing CatalogUrl")));

    services.AddHttpClient<IBasketService, BasketService>(c =>
      c.BaseAddress = new Uri(Configuration["ApiSettings:BasketUrl"] ?? throw Errors.ServerSide.ConfigurationMissing("Missing BasketUrl")));

    services.AddHttpClient<IOrderService, OrderService>(c =>
      c.BaseAddress = new Uri(Configuration["ApiSettings:OrderingUrl"] ?? throw Errors.ServerSide.ConfigurationMissing("Missing OrderingUrl")));

    services.AddControllers();
    services.AddHealthChecks();
    services.AddSwaggerGen(c =>
    {
      c.SwaggerDoc("v1", new OpenApiInfo { Title = "Shopping.Aggregator", Version = "v1" });
    });
  }

  // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
  public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
  {
    if (env.IsDevelopment())
    {
      app.UseDeveloperExceptionPage();
      app.UseSwagger();
      app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Shopping.Aggregator v1"));
    }

    app.UseMiddleware<RequestContextMiddleware>();
    // Global exception handler for the service (returns SharedKernel.Errors.Error payload)
    app.UseGlobalExceptionHandler(Constants.ServiceCodes.ShoppingAggregator);

        app.UseRouting();

    app.UseAuthorization();

    app.UseEndpoints(endpoints =>
    {
      endpoints.MapHealthChecks("/health");
      endpoints.MapControllers();
    });
  }
}
