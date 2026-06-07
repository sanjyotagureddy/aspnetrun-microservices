namespace LogStore.Api.Features.PayloadProtection;

internal static class PayloadProtectionRouteGroupExtensions
{
    public static RouteGroupBuilder MapLogStorageV1(this IEndpointRouteBuilder app)
    {
        return app.MapGroup("/api/v1/logs")
            .WithTags("LogStorage");
    }
}
