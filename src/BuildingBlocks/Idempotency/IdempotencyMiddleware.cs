using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;

namespace BuildingBlocks.Idempotency;

// Lightweight example: replace IMemoryCache with a persistent store in production.
public sealed class IdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;

    public IdempotencyMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var key) || string.IsNullOrWhiteSpace(key))
        {
            await _next(context);
            return;
        }

        var cacheKey = $"idem:{key}";
        if (_cache.TryGetValue(cacheKey, out byte[] existing))
        {
            context.Response.ContentType = "application/json";
            await context.Response.Body.WriteAsync(existing);
            return;
        }

        // capture response
        var originalBody = context.Response.Body;
        await using var memStream = new MemoryStream();
        context.Response.Body = memStream;

        await _next(context);

        memStream.Position = 0;
        var responseBytes = memStream.ToArray();

        // store a copy with a sensible TTL; production should persist backed by DB
        _cache.Set(cacheKey, responseBytes, TimeSpan.FromMinutes(10));

        memStream.Position = 0;
        await memStream.CopyToAsync(originalBody);
        context.Response.Body = originalBody;
    }
}
