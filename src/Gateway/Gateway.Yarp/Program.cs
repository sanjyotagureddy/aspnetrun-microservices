using Common.SharedKernel.Logging;
using Common.SharedKernel.Observability.Context;
using Gateway.Yarp.Observability;
using Serilog;

namespace Gateway.Yarp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        builder.Host.UseSerilog(
            (context, services, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console(),
            writeToProviders: true);

        builder.Services.AddConfiguredCommonSharedKernelLogging(builder.Configuration, "gateway-yarp");
        builder.Services.UseRequestLoggingMiddleware<GatewayRequestLoggingMiddleware>();

        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
            .AddServiceDiscoveryDestinationResolver();

        builder.Services.AddAuthorization();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        app.MapDefaultEndpoints();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseAppCallContextMiddleware<AppCallContextMiddleware>();
        app.UseAuthorization();

        app.MapReverseProxy();

        app.Run();
    }
}
