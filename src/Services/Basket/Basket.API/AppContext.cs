using System.Diagnostics.CodeAnalysis;

using SharedKernel.Context;

namespace Basket.API;

/// <inheritdoc />
[ExcludeFromCodeCoverage]
public class AppContext(string serviceName) : RequestContext(serviceName)
{
    public new static AppContext Current
    {
        get
        {
            RequestContext current = RequestContextScope.Current;
            return current as AppContext ?? FromRequestContext(current);
        }
    }

    public new static AppContext FromHttpContext(HttpContext httpContext, string serviceName)
    {
        var requestContext = RequestContext.FromHttpContext(httpContext, serviceName);
        return FromRequestContext(requestContext);
    }

    private static AppContext FromRequestContext(RequestContext context)
    {
        return new AppContext(context.ServiceName)
        {
            CorrelationId = context.CorrelationId,
            TransactionId = context.TransactionId,
            RequestId = context.RequestId,
            TraceId = context.TraceId,
            TenantId = context.TenantId,
            SessionId = context.SessionId,
            RequestStartTimestampUtc = context.RequestStartTimestampUtc,
            Headers = context.Headers
        };
    }
}