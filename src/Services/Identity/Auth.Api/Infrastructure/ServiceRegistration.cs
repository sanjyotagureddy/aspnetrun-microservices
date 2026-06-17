using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Auth.Api.Infrastructure.Configuration;
using Common.SharedKernel.Logging;
using Auth.Api.Observability;
using Auth.Api.Infrastructure.Health;
using Auth.Api.Infrastructure.Persistence;
using Auth.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Auth.Api.Infrastructure;

[ExcludeFromCodeCoverage]
internal static class ServiceRegistration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddAuthApi(IConfiguration configuration)
        {
            services.AddAuthCoreInfrastructure();
            services.AddAuthPersistence(configuration);
            services.AddAuthAuthentication();
            services.AddAuthAuthorization();
            services.AddAuthHealthChecks();
            services.AddValidationBehaviour();
            services.AddAuthObservability(configuration);
            services.AddAuthApplicationLayer();
            services.AddAuthExceptionHandling();

            return services;
        }

        private void AddAuthCoreInfrastructure()
        {
            services.AddSingleton(TimeProvider.System);
            services.AddScoped<ITenantAuthorizationService, TenantAuthorizationService>();
            services.AddOptions<AuthOptions>()
                .BindConfiguration("Auth")
                .Validate(AuthOptionsValidation.HasRequiredSettings, "Auth settings are missing required or invalid authority/issuer/discovery/jwks/audience values.")
                .Validate(AuthOptionsValidation.HasValidPkceClients, "Auth:PkceClients configuration is invalid. Configure unique client IDs and absolute redirect URIs.")
                .ValidateOnStart();

            services.AddOptions<WorkloadAuthOptions>()
                .BindConfiguration("WorkloadAuth")
                .Validate(WorkloadAuthOptionsValidation.HasValidClients, "WorkloadAuth:Clients must contain unique client IDs and non-empty allowed scopes.")
                .ValidateOnStart();

            services.AddOptions<DevBootstrapOptions>()
                .BindConfiguration("DevBootstrap")
                .Validate<IHostEnvironment>(DevBootstrapOptionsValidation.IsValidForEnvironment, "DevBootstrap is invalid. Enable it only in Development and provide a non-empty SharedSecret.")
                .ValidateOnStart();
        }

        private void AddAuthPersistence(IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("authdb")
                ?? throw new InvalidOperationException("ConnectionStrings:authdb is not configured.");

            services.AddSingleton(_ =>
            {
                NpgsqlDataSourceBuilder builder = new(connectionString);
                return builder.Build();
            });

            services.AddDbContext<AuthDbContext>((serviceProvider, options) =>
            {
                NpgsqlDataSource dataSource = serviceProvider.GetRequiredService<NpgsqlDataSource>();
                options.UseNpgsql(dataSource);
            });
        }

        private void AddAuthAuthentication()
        {
            services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<AuthOptions>>((options, authOptionsAccessor) =>
                {
                    AuthOptions authOptions = authOptionsAccessor.Value;
                    options.Authority = authOptions.Authority;
                    options.Audience = authOptions.Audience;
                    options.RequireHttpsMetadata = false;
                });
        }

        private void AddAuthAuthorization()
        {
            services.AddAuthorizationBuilder()
                .AddPolicy(AuthPolicyNames.WorkloadOnly, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireAssertion(context => IsWorkloadPrincipal(context.User));
                })
                .AddPolicy(AuthPolicyNames.UserOnly, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireAssertion(context => IsUserPrincipal(context.User));
                });
        }

        private static bool IsWorkloadPrincipal(ClaimsPrincipal user)
        {
            string? subject = user.FindFirstValue("sub");
            string? clientId = user.FindFirstValue("azp") ?? user.FindFirstValue("client_id");

            bool hasClientId = !string.IsNullOrWhiteSpace(clientId);
            bool isServiceAccount = !string.IsNullOrWhiteSpace(subject)
                && subject.StartsWith("service-account-", StringComparison.OrdinalIgnoreCase);

            return hasClientId && (isServiceAccount || string.IsNullOrWhiteSpace(subject));
        }

        private static bool IsUserPrincipal(ClaimsPrincipal user)
        {
            string? subject = user.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(subject))
            {
                return false;
            }

            return !subject.StartsWith("service-account-", StringComparison.OrdinalIgnoreCase);
        }

        private void AddAuthHealthChecks()
        {
            services.AddHttpClient("auth-authority-health", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(5);
            });

            services.AddHealthChecks()
                .AddCheck<AuthDatabaseHealthCheck>("auth-db", HealthStatus.Unhealthy, ["ready"])
                .AddCheck<AuthAuthorityHealthCheck>("auth-authority", HealthStatus.Unhealthy, ["ready"])
                .AddCheck<AuthCacheHealthCheck>("auth-cache-config", HealthStatus.Degraded, ["ready"]);
        }

        private void AddAuthObservability(IConfiguration configuration)
        {
            services.AddConfiguredCommonSharedKernelLogging(configuration, "auth-api");
            services.UseRequestLoggingMiddleware<AuthRequestLoggingMiddleware>();
        }

        private void AddAuthApplicationLayer()
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            });
        }

        private void AddAuthExceptionHandling()
        {
            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails();
        }

        private void AddValidationBehaviour()
        {
            services.AddValidatorsFromAssemblyContaining<Program>();
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(Common.SharedKernel.Validation.ValidationBehavior<,>));
        }
    }
}
