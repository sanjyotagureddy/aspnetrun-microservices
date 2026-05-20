using Discount.API.Repositories;
using Discount.API.Repositories.Interfaces;
using Microsoft.OpenApi;

namespace Discount.API;

public class Startup(IConfiguration configuration)
{
  public IConfiguration Configuration { get; } = configuration;

  // This method gets called by the runtime. Use this method to add services to the container.
  public void ConfigureServices(IServiceCollection services)
  {
    services.AddScoped<IDiscountRepository, DiscountRepository>();

    services.AddControllers();
    services.AddSwaggerGen(c => { c.SwaggerDoc("v1", new OpenApiInfo { Title = "Discount.API", Version = "v1" }); });
  }

  // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
  public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
  {
    if (env.IsDevelopment())
    {
      app.UseDeveloperExceptionPage();
      app.UseSwagger();
      app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Discount.API v1"));
    }

    app.UseRouting();

    app.UseAuthorization();

    app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
  }
}