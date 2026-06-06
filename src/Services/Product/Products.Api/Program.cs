using System.Threading.RateLimiting;
using Common.SharedKernel.Observability.Context;

using Microsoft.AspNetCore.RateLimiting;

using Products.Api.Observability;
using Serilog;

namespace Products.Api;

public class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        builder.Host.UseSerilog(
            (context, services, loggerConfiguration) => loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console(),
            writeToProviders: true);

        builder.Services.AddAuthorization();
        builder.Services.AddSwaggerSupport(builder.Configuration, "Products API");
        // Register services
        builder.Services.AddResponseCompression();
        
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Default", policy =>
                policy.WithOrigins().AllowAnyMethod().AllowAnyHeader());
        });
        builder.Services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("default", opts =>
            {
                opts.PermitLimit = 100;
                opts.Window = TimeSpan.FromMinutes(1);
                opts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opts.QueueLimit = 0;
            });
        });
        
        builder.Services.AddOutputCache();
        

        builder.Services.AddProductsApi(builder.Configuration);


        WebApplication app = builder.Build();

        app.UseSwaggerSupport("Products API");
        app.UseForwardedHeaders();
        app.UseAppCallContextMiddleware<AppCallContextMiddleware>();
        app.UseExceptionHandler(); // early
        if (!app.Environment.IsDevelopment()) app.UseHsts();
        app.UseHttpsRedirection();
        app.UseResponseCompression();
        app.UseSerilogRequestLogging(); // or your logging middleware
        app.UseCors("Default");
        app.UseRateLimiter();
        app.UseAuthorization();
        app.MapDiscoveredEndpoints();
        app.UseOutputCache(); // if using endpoint-level caching, otherwise configure policies
        app.Run();
    }
}
