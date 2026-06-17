
namespace Gateway.Yarp;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure(options =>
            {
                options.Authority = builder.Configuration["Auth:Authority"];
                options.Audience = builder.Configuration["Auth:Audience"] ?? "gateway-yarp";
                options.RequireHttpsMetadata = false;
                options.TokenValidationParameters.ValidIssuer = builder.Configuration["Auth:Issuer"];
                options.TokenValidationParameters.ValidAudience = builder.Configuration["Auth:Audience"] ?? "gateway-yarp";
            });

        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("GatewayProductsReadPolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("sub");
                policy.RequireClaim("tenant_id");
                policy.RequireAssertion(context => HasScope(context.User, "products.read"));
            })
            .AddPolicy("GatewayProductsWritePolicy", policy =>
            {
                policy.RequireAuthenticatedUser();
                policy.RequireClaim("sub");
                policy.RequireClaim("tenant_id");
                policy.RequireAssertion(context => HasScope(context.User, "products.write"));
            });

        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        app.MapStandardHealthEndpoints();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapReverseProxy();

        app.Run();
    }

    private static bool HasScope(ClaimsPrincipal user, string requiredScope)
    {
        return user.FindAll("scope")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(scope => string.Equals(scope, requiredScope, StringComparison.Ordinal));
    }
}
