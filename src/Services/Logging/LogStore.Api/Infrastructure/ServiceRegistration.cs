using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;

namespace LogStore.Api.Infrastructure;

[ExcludeFromCodeCoverage]
internal static class ServiceRegistration
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddPayloadProtectionApi(IConfiguration configuration)
        {
            IConfigurationSection openSearchSection = configuration.GetSection("LogStorage:OpenSearch");

            services.AddOptions<LogStorageOptions>()
                .Configure(options =>
                {
                    options.Endpoint = Uri.TryCreate(openSearchSection["Endpoint"], UriKind.Absolute, out Uri? endpoint)
                        ? endpoint
                        : new Uri("http://localhost:9200");
                    options.ApiIndexPrefix = string.IsNullOrWhiteSpace(openSearchSection["ApiIndexPrefix"])
                        ? "api-logs"
                        : openSearchSection["ApiIndexPrefix"]!;
                    options.InfraIndexPrefix = string.IsNullOrWhiteSpace(openSearchSection["InfraIndexPrefix"])
                        ? "infra-logs"
                        : openSearchSection["InfraIndexPrefix"]!;
                    options.MessagingIndexPrefix = string.IsNullOrWhiteSpace(openSearchSection["MessagingIndexPrefix"])
                        ? "messaging-log"
                        : openSearchSection["MessagingIndexPrefix"]!;
                    options.AuditIndexPrefix = string.IsNullOrWhiteSpace(openSearchSection["AuditIndexPrefix"])
                        ? "audit-log"
                        : openSearchSection["AuditIndexPrefix"]!;
                    options.SecurityIndexPrefix = string.IsNullOrWhiteSpace(openSearchSection["SecurityIndexPrefix"])
                        ? "security-log"
                        : openSearchSection["SecurityIndexPrefix"]!;
                    options.UseDailyIndexes = !bool.TryParse(openSearchSection["UseDailyIndexes"], out bool useDaily)
                        || useDaily;
                });

            services.AddHttpClient<ILogStorageService, PayloadProtectionService>((serviceProvider, client) =>
            {
                LogStorageOptions options = serviceProvider.GetRequiredService<IOptions<LogStorageOptions>>().Value;
                client.BaseAddress = options.Endpoint;
            });

            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails();

            return services;
        }
    }
}
