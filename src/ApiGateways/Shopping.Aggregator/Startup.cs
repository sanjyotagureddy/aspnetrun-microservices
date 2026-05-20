using Microsoft.OpenApi;
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
      c.BaseAddress = new Uri(Configuration["ApiSettings:CatalogUrl"]));

    services.AddHttpClient<IBasketService, BasketService>(c =>
      c.BaseAddress = new Uri(Configuration["ApiSettings:BasketUrl"]));

    services.AddHttpClient<IOrderService, OrderService>(c =>
      c.BaseAddress = new Uri(Configuration["ApiSettings:OrderingUrl"]));

    services.AddControllers();
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

    app.UseRouting();

    app.UseAuthorization();

    app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
  }
}