using Serilog;
using Common.SharedKernel.Observability.Context;

using Inventory.Api.Observability;

namespace Inventory.Api;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        builder.Host.UseSerilog(
            (context, services, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console(),
            writeToProviders: true);

        builder.Services.AddAuthorization();
        builder.Services.AddSwaggerSupport(builder.Configuration, "Inventory API");
        builder.Services.AddInventoryApi(builder.Configuration);

        WebApplication app = builder.Build();

        app.MapDefaultEndpoints();
        app.UseSwaggerSupport("Inventory");
        app.UseForwardedHeaders();
        app.UseAppCallContextMiddleware<AppCallContextMiddleware>();
        app.UseExceptionHandler();

        if (!app.Environment.IsDevelopment()) app.UseHsts();

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapDiscoveredEndpoints();

        app.Run();
    }
}
