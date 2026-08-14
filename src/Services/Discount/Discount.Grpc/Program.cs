using Common.SharedKernel.Logging;
using Common.SharedKernel.Observability.Context;
using Discount.Grpc.Data;
using Discount.Grpc.Observability;
using Discount.Grpc.Services;
using Serilog;

namespace Discount.Grpc;

public class Program
{
    public static async Task Main(string[] args)
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

        builder.Services.AddConfiguredCommonSharedKernelLogging(builder.Configuration, "discount-grpc");
        builder.Services.UseRequestLoggingMiddleware<DiscountRequestLoggingMiddleware>();

        // Add services to the container.
        builder.Services.AddGrpc();
        builder.Services.AddScoped<IDiscountRepository, DiscountRepository>();

        var app = builder.Build();

        // Initialize SQLite DB
        await DiscountDbInitializer.InitializeDbAsync(app.Configuration, app.Logger);

        app.MapDefaultEndpoints();
        app.UseAppCallContextMiddleware<AppCallContextMiddleware>();

        // Configure the HTTP request pipeline.
        app.MapGrpcService<DiscountService>();
        app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");

        await app.RunAsync();
    }
}
