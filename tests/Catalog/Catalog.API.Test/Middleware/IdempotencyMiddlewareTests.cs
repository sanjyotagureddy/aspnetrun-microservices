using System.Text;
using System.Text.Json;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;

using Moq;
using SharedKernel.Middleware;
using Xunit;

namespace Catalog.API.Test.Middleware;

public class IdempotencyMiddlewareTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task InvokeAsync_ForGetRequests_BypassesCacheAndCallsNext()
    {
        var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
        var middleware = new IdempotencyMiddleware(context =>
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        }, cache.Object);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public async Task InvokeAsync_WhenIdempotencyKeyMissing_CallsNext()
    {
        var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
        var middleware = new IdempotencyMiddleware(context =>
        {
            context.Response.StatusCode = StatusCodes.Status201Created;
            return Task.CompletedTask;
        }, cache.Object);
        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public async Task InvokeAsync_WhenCachedResponseExists_ReplaysCachedResponse()
    {
        var cache = new Mock<IDistributedCache>();
        var payload = JsonSerializer.SerializeToUtf8Bytes(new
        {
            statusCode = StatusCodes.Status201Created,
            contentType = "application/json",
            location = "/api/v1/catalog/products/1",
            body = Encoding.UTF8.GetBytes("{\"id\":1}")
        }, SerializerOptions);

        cache.Setup(c => c.GetAsync("catalog-idem:idem-1", It.IsAny<CancellationToken>())).ReturnsAsync(payload);

        var middleware = new IdempotencyMiddleware(_ =>
        {
            throw new InvalidOperationException("next should not be called");
        }, cache.Object);

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Headers["Idempotency-Key"] = "idem-1";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
        context.Response.ContentType.Should().Be("application/json");
        context.Response.Headers.Location.ToString().Should().Be("/api/v1/catalog/products/1");
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        (await reader.ReadToEndAsync()).Should().Be("{\"id\":1}");
    }

    [Fact]
    public async Task InvokeAsync_WhenResponseIsNotCached_StoresResponseAndCopiesBody()
    {
        var cache = new Mock<IDistributedCache>();
        cache.Setup(c => c.GetAsync("catalog-idem:idem-2", It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        cache.Setup(c => c.SetAsync(
                "catalog-idem:idem-2",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        var middleware = new IdempotencyMiddleware(context =>
        {
            context.Response.StatusCode = StatusCodes.Status201Created;
            context.Response.ContentType = "application/json";
            context.Response.Headers.Location = "/api/v1/catalog/products/2";
            return context.Response.WriteAsync("{\"id\":2}");
        }, cache.Object);

        var context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Headers["Idempotency-Key"] = "idem-2";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        cache.Verify();
        context.Response.StatusCode.Should().Be(StatusCodes.Status201Created);
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);
        (await reader.ReadToEndAsync()).Should().Be("{\"id\":2}");
    }
}
