using Common.SharedKernel;
using Common.SharedKernel.Logging;
using Common.SharedKernel.Observability.Context;
using Microsoft.Extensions.Options;

namespace Gateway.Yarp.Observability;

internal sealed class GatewayRequestLoggingMiddleware(
    RequestDelegate next,
    Common.SharedKernel.Logging.ILogger<RequestLoggingMiddlewareBase> logger,
    ILogContextAccessor contextAccessor,
    IOptions<LoggingMiddlewareOptions> middlewareOptions,
    IPayloadProtectionPipeline payloadProtectionPipeline,
    IPayloadStore payloadStore)
    : RequestLoggingMiddlewareBase(
        next,
        logger,
        contextAccessor,
        middlewareOptions,
        payloadProtectionPipeline,
        payloadStore)
{
    protected override void EnrichCompletionProperties(HttpContext httpContext, IDictionary<string, object?> completionProperties, Exception? exception)
    {
        completionProperties["api.application"] = "gateway-yarp";

        AppCallContextBase? appContext = AppCallContextBase.Current;
        if (appContext is null)
        {
            return;
        }

        completionProperties["api.call.application"] = appContext.ApplicationName;
        completionProperties["api.call.parentCorrelationId"] = appContext.ParentCorrelationId;

        if (appContext.Headers.TryGetValue(Constants.Headers.CallerService, out string? callerService)
            && !string.IsNullOrWhiteSpace(callerService))
        {
            completionProperties["api.call.callerService"] = callerService;
        }
    }
}
