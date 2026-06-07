using Serilog;

namespace LogStore.Api;

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
        builder.Services.AddSwaggerSupport(builder.Configuration, "Log Storage API");
        builder.Services.AddPayloadProtectionApi(builder.Configuration);

        WebApplication app = builder.Build();

        app.MapDefaultEndpoints();
        app.UseSwaggerSupport("Log Storage API");
        app.UseForwardedHeaders();
        app.UseExceptionHandler();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseAuthorization();
        app.MapDiscoveredEndpoints();

        app.Run();
    }
}
