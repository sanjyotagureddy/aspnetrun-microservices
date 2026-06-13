using Auth.Api.Observability;
using Auth.Api.Infrastructure.Persistence;
using Serilog;
using Common.SharedKernel.Observability.Context;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

namespace Auth.Api;

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

        builder.Services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
        builder.Services.AddAuthorization();
        builder.Services.AddSwaggerSupport(builder.Configuration, "Auth API");
        builder.Services.AddAuthApi(builder.Configuration);

        WebApplication app = builder.Build();

        using (IServiceScope scope = app.Services.CreateScope())
        {
            AuthDbContext dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
            dbContext.Database.Migrate();
        }

        app.MapStandardHealthEndpoints(readyUsesReadyTag: true);
        app.UseSwaggerSupport("Auth API");
        app.UseForwardedHeaders();
        app.UseAppCallContextMiddleware<AppCallContextMiddleware>();
        app.UseExceptionHandler();

        if (!app.Environment.IsDevelopment()) app.UseHsts();

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapDiscoveredEndpoints();
        app.Run();
    }
}
