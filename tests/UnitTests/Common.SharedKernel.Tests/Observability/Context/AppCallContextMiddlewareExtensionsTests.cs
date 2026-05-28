using Common.SharedKernel.Observability.Context;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Common.SharedKernel.Tests.Observability.Context;

public sealed class AppCallContextMiddlewareExtensionsTests
{
    [Fact]
    public async Task UseAppCallContextMiddleware_ShouldRegisterDerivedMiddlewareIntoPipeline()
    {
        var services = new ServiceCollection();
        var appBuilder = new ApplicationBuilder(services.BuildServiceProvider());
        bool terminalInvoked = false;

        appBuilder.UseAppCallContextMiddleware<TestAppCallContextMiddleware>();
        appBuilder.Run(context =>
        {
            terminalInvoked = true;

            var current = AppCallContextBase.CurrentAs<TestAppCallContext>();

            Assert.NotNull(current);
            Assert.Equal("corr-123", current.CorrelationId);
            Assert.Equal("corr-123", current.ApplicationName);
            Assert.Equal("ORD-123", current.OrderId);
            Assert.Equal("/orders/123", current.Items["requestPath"]);

            context.Response.StatusCode = StatusCodes.Status204NoContent;
            return Task.CompletedTask;
        });

        var pipeline = appBuilder.Build();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["x-correlation-id"] = "corr-123";
        httpContext.Request.Path = "/orders/123";

        await pipeline(httpContext);

        Assert.True(terminalInvoked);
        Assert.Equal(StatusCodes.Status204NoContent, httpContext.Response.StatusCode);
        Assert.Null(AppCallContextBase.Current);
    }

    private sealed class TestAppCallContextMiddleware(RequestDelegate next)
        : AppCallContextMiddleware<TestAppCallContext>(next, BuildContext)
    {
        protected override void ConfigureContext(HttpContext httpContext, TestAppCallContext context)
        {
            context.Items["requestPath"] = httpContext.Request.Path.Value ?? string.Empty;
        }

        private static TestAppCallContext BuildContext(HttpContext httpContext)
        {
            return new TestAppCallContext(
                correlationId: httpContext.Request.Headers["x-correlation-id"].ToString(),
                orderId: "ORD-123");
        }
    }

    private sealed class TestAppCallContext(
        string correlationId,
        string orderId,
        string? parentCorrelationId = null,
        string? traceId = null,
        string? spanId = null,
        IDictionary<string, string>? headers = null,
        IDictionary<string, object?>? items = null)
        : AppCallContextBase(correlationId, parentCorrelationId, traceId, spanId, headers, items)
    {
        public string OrderId { get; } = orderId;
    }
}