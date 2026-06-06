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

        // Configure Serilog early so host logs are captured.
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        builder.Host.UseSerilog();

        builder.Services.AddAuthorization();
        builder.Services.AddSwaggerSupport(builder.Configuration, "Inventory API");
        builder.Services.AddInventoryApi(builder.Configuration);

        WebApplication app = builder.Build();

        app.MapDefaultEndpoints();
        app.UseSwaggerSupport("Inventory API");
        app.UseForwardedHeaders();
        app.UseAppCallContextMiddleware<AppCallContextMiddleware>();
        app.UseExceptionHandler();

        if (!app.Environment.IsDevelopment()) app.UseHsts();

        app.UseHttpsRedirection();
        app.UseSerilogRequestLogging();
        app.UseAuthorization();
        app.MapDiscoveredEndpoints();

        app.Run();
    }
}
