namespace Common.SharedKernel.Logging;

internal sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddlewareBase> logger,
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
}
