namespace Common.SharedKernel.Logging;

internal sealed class RequestLoggingMiddlewareRegistration
{
    public RequestLoggingMiddlewareRegistration(Type middlewareType)
    {
        if (!typeof(RequestLoggingMiddlewareBase).IsAssignableFrom(middlewareType))
        {
            throw new ArgumentException(
                $"Middleware type '{middlewareType.FullName}' must inherit from {nameof(RequestLoggingMiddlewareBase)}.",
                nameof(middlewareType));
        }

        MiddlewareType = middlewareType;
    }

    public Type MiddlewareType { get; }
}