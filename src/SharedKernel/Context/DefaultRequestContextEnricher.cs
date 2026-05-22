namespace SharedKernel.Context;

public sealed class DefaultRequestContextEnricher : IRequestContextEnricher
{
    public void EnrichRequest(HttpContext httpContext, RequestContext requestContext)
    {
        httpContext.Request.Headers[Constants.Headers.CorrelationId] = requestContext.CorrelationId;
        httpContext.Request.Headers[Constants.Headers.TransactionId] = requestContext.TransactionId;
        httpContext.Request.Headers[Constants.Headers.RequestId] = requestContext.RequestId;
    }

    public void EnrichResponse(HttpContext httpContext, RequestContext requestContext)
    {
        httpContext.Response.Headers[Constants.Headers.CorrelationId] = requestContext.CorrelationId;
        httpContext.Response.Headers[Constants.Headers.TransactionId] = requestContext.TransactionId;
        httpContext.Response.Headers[Constants.Headers.RequestId] = requestContext.RequestId;
    }

    
}
