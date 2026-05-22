namespace SharedKernel.Context;

public interface IRequestContextEnricher
{
    void EnrichRequest(HttpContext httpContext, RequestContext appContext);

    void EnrichResponse(HttpContext httpContext, RequestContext appContext);
}
