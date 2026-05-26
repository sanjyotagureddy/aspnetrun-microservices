using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

using Moq;

using Ordering.API.Middlewares;

using SharedKernel;
using SharedKernel.Context;

using Xunit;

namespace Ordering.API.Test;

public sealed class RequestContextMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_CreatesContextAndRunsEnrichers()
    {
        var hostEnvironment = new Mock<IHostEnvironment>(MockBehavior.Strict);
        hostEnvironment.SetupGet(value => value.ApplicationName).Returns("Ordering.API");

        var enricher = new RecordingEnricher();
        RequestContext captured = null;

        var middleware = new RequestContextMiddleware(
            context =>
            {
                captured = RequestContextScope.Current;
                context.Response.StatusCode = StatusCodes.Status200OK;
                return context.Response.StartAsync();
            },
            hostEnvironment.Object,
            new[] { enricher });

        var httpContext = new DefaultHttpContext();

        await middleware.InvokeAsync(httpContext);

        Assert.NotNull(captured);
        Assert.Equal("Ordering.API", captured.ServiceName);
        Assert.Equal(1, enricher.RequestCalls);
        Assert.True(httpContext.Response.Headers.ContainsKey(Constants.Headers.CorrelationId));
    }

    private sealed class RecordingEnricher : IRequestContextEnricher
    {
        public int RequestCalls { get; private set; }

        public int ResponseCalls { get; private set; }

        public void EnrichRequest(HttpContext httpContext, RequestContext appContext)
        {
            RequestCalls++;
        }

        public void EnrichResponse(HttpContext httpContext, RequestContext appContext)
        {
            ResponseCalls++;
        }
    }
}