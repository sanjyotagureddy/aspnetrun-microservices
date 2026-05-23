using System.Diagnostics.CodeAnalysis;

using Ocelot.Cache.CacheManager;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

namespace OcelotApiGateway;

[ExcludeFromCodeCoverage]
public class Startup
{
  // This method gets called by the runtime. Use this method to add services to the container.
  // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
  public void ConfigureServices(IServiceCollection services)
  {
    services.AddHealthChecks();
    services.AddOcelot()
      .AddCacheManager(settings => settings.WithDictionaryHandle());
  }

  // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
  public async void Configure(IApplicationBuilder app, IWebHostEnvironment env)
  {
    if (env.IsDevelopment()) app.UseDeveloperExceptionPage();

    app.UseRouting();

    app.UseEndpoints(endpoints =>
    {
      endpoints.MapGet("/", async context => { await context.Response.WriteAsync("Hello World!"); });
      endpoints.MapHealthChecks("/health");
    });

    await app.UseOcelot();
  }
}
