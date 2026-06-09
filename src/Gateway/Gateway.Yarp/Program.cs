
using System.Threading.RateLimiting;
using Common.SharedKernel.Observability.Context;
using Gateway.Yarp.Observability;
using Gateway.Yarp.Security;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;

namespace Gateway.Yarp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        // Add services to the container.
        builder.Services.AddAuthentication(ApiKeyAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(
                ApiKeyAuthenticationHandler.SchemeName,
                _ =>
                {
                });

        builder.Services.AddAuthorization();
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("GatewayApiKeyPolicy", policy =>
            {
                policy.AddAuthenticationSchemes(ApiKeyAuthenticationHandler.SchemeName);
                policy.RequireAuthenticatedUser();
            });

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddFixedWindowLimiter("public-read", limiterOptions =>
            {
                limiterOptions.PermitLimit = 300;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0;
            });

            options.AddFixedWindowLimiter("protected-write", limiterOptions =>
            {
                limiterOptions.PermitLimit = 100;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0;
            });
        });

        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        app.MapDefaultEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAppCallContextMiddleware<AppCallContextMiddleware>();
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();

        app.MapReverseProxy();

        app.Run();
    }
}
