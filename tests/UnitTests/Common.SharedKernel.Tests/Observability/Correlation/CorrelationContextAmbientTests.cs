using Common.SharedKernel.Observability.Context;
using Microsoft.AspNetCore.Http;

namespace Common.SharedKernel.Tests.Observability.Correlation;

public sealed class AppCallContextAmbientTests
{
    [Fact]
    public async Task Current_ShouldFlowAcrossAsyncCallsInsideScope()
    {
        var ctx = new OrderAppCallContext(correlationId: "corr-1", orderId: "ORD-1");
        using var _ = AppCallContextBase.BeginScope(ctx);

        await Task.Yield();
        Assert.Equal("corr-1", AppCallContextBase.CurrentAs<OrderAppCallContext>()?.CorrelationId);

        var result = await Task.Run(() => AppCallContextBase.CurrentAs<OrderAppCallContext>()?.CorrelationId);
        Assert.Equal("corr-1", result);
    }

    [Fact]
    public void CurrentAs_ShouldExposeDerivedContextProperties()
    {
        var orderContext = new OrderAppCallContext(
            correlationId: "corr-order",
            orderId: "ORD-123");

        using var _ = AppCallContextBase.BeginScope(orderContext);

        Assert.Equal("ORD-123", AppCallContextBase.CurrentAs<OrderAppCallContext>()?.OrderId);
    }

    [Fact]
    public void Scope_Dispose_ShouldRestorePreviousContext()
    {
        using (AppCallContextBase.BeginScope(new OrderAppCallContext("parent", "ORD-P")))
        {
            using (AppCallContextBase.BeginScope(new OrderAppCallContext("child", "ORD-C")))
            {
                Assert.Equal("child", AppCallContextBase.CurrentAs<OrderAppCallContext>()?.CorrelationId);
            }

            Assert.Equal("parent", AppCallContextBase.CurrentAs<OrderAppCallContext>()?.CorrelationId);
        }

        Assert.Null(AppCallContextBase.Current);
    }

    [Fact]
    public void WithHeader_ShouldAddHeaderAndNotMutateOriginal()
    {
        var ctx = new OrderAppCallContext("c1", "ORD-10");
        ctx.Headers["Existing"] = "1";

        var updated = new OrderAppCallContext(ctx.CorrelationId, ctx.OrderId, headers: ctx.Headers);
        updated.Headers["X-User"] = "abc";

        Assert.False(ctx.Headers.ContainsKey("X-User"));
        Assert.True(updated.Headers.ContainsKey("X-User"));
        Assert.Equal("abc", updated.Headers["X-User"]);
    }

    [Fact]
    public async Task Middleware_ShouldInitializeScopeFromHeaders()
    {
        var middleware = new AppCallContextMiddlewareBase<OrderAppCallContext>(
            async context =>
            {
                var current = AppCallContextBase.CurrentAs<OrderAppCallContext>();
                Assert.Equal("corr-from-header", current?.CorrelationId);
                Assert.Equal("parent-1", current?.ParentCorrelationId);
                Assert.True(current?.Headers.ContainsKey("x-correlation-id"));
                await Task.CompletedTask;
            },
            httpContext =>
            {
                var correlationId = httpContext.Request.Headers["x-correlation-id"].ToString();
                var parentCorrelationId = httpContext.Request.Headers["x-parent-correlation-id"].ToString();
                var headers = httpContext.Request.Headers.ToDictionary(
                    header => header.Key,
                    header => header.Value.ToString(),
                    StringComparer.OrdinalIgnoreCase);

                return new OrderAppCallContext(
                    correlationId,
                    orderId: "ORD-333",
                    parentCorrelationId: parentCorrelationId,
                    traceId: httpContext.TraceIdentifier,
                    headers: headers);
            });

        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "trace-default";
        httpContext.Request.Headers["x-correlation-id"] = "corr-from-header";
        httpContext.Request.Headers["x-parent-correlation-id"] = "parent-1";

        await middleware.InvokeAsync(httpContext);

        Assert.Null(AppCallContextBase.Current);
    }

    [Fact]
    public async Task Middleware_ShouldAllowCustomDerivedContextFactory()
    {
        var middleware = new AppCallContextMiddlewareBase<OrderAppCallContext>(
            async _ =>
            {
                var current = AppCallContextBase.CurrentAs<OrderAppCallContext>();
                Assert.Equal("ORD-999", current?.OrderId);
                await Task.CompletedTask;
            },
            httpContext => new OrderAppCallContext(
                correlationId: httpContext.TraceIdentifier,
                orderId: "ORD-999"));

        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "trace-custom";

        await middleware.InvokeAsync(httpContext);

        Assert.Null(AppCallContextBase.Current);
    }

    private sealed class OrderAppCallContext(
        string correlationId,
        string orderId,
        string? parentCorrelationId = null,
        string? traceId = null,
        string? spanId = null,
        IDictionary<string, string>? headers = null,
        IDictionary<string, object?>? items = null) : AppCallContextBase(correlationId, parentCorrelationId, traceId, spanId, headers, items)
    {
        public string OrderId { get; } = orderId;
    }
}
