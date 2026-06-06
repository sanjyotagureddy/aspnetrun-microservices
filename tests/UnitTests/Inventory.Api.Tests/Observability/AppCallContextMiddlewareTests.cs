using Common.SharedKernel.Observability.Context;
using Inventory.Api.Observability;
using Microsoft.AspNetCore.Http;

namespace Inventory.Api.Tests.Observability;

public sealed class AppCallContextMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldPopulateAmbientContext_AndResponseCorrelationHeader()
    {
        AppCallContext? captured = null;
        AppCallContextMiddleware middleware = new(async context =>
        {
            captured = AppCallContextBase.CurrentAs<AppCallContext>();
            await Task.CompletedTask;
        });

        DefaultHttpContext httpContext = new();
        httpContext.Request.Method = "GET";
        httpContext.Request.Path = "/api/v1/inventory";
        httpContext.Request.Headers["X-Correlation-Id"] = "corr-1";
        httpContext.Request.Headers["X-Tenant-Id"] = "tenant-1";

        await middleware.InvokeAsync(httpContext);

        Assert.NotNull(captured);
        Assert.Equal("corr-1", captured!.CorrelationId);
        Assert.Equal("tenant-1", captured.TenantId);
        Assert.Equal("GET", captured.Items["method"]);
        Assert.Equal("/api/v1/inventory", captured.Items["requestPath"]);
        Assert.Equal("corr-1", httpContext.Response.Headers[Common.SharedKernel.Constants.Headers.CorrelationId]);
    }
}
