using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Catalog.API.Middleware;

internal sealed class IdempotencyMiddleware(RequestDelegate next, IDistributedCache cache)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly IDistributedCache _cache = cache ?? throw new ArgumentNullException(nameof(cache));

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldApply(context.Request))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("Idempotency-Key", out var key) || string.IsNullOrWhiteSpace(key))
        {
            await _next(context);
            return;
        }

        var cacheKey = $"catalog-idem:{key}";
        var cachedResponse = await _cache.GetAsync(cacheKey, context.RequestAborted);
        if (cachedResponse is not null)
        {
            var entry = JsonSerializer.Deserialize<CachedResponse>(cachedResponse, SerializerOptions);
            if (entry is not null)
            {
                context.Response.StatusCode = entry.StatusCode;
                if (!string.IsNullOrWhiteSpace(entry.ContentType))
                    context.Response.ContentType = entry.ContentType;

                if (!string.IsNullOrWhiteSpace(entry.Location))
                    context.Response.Headers.Location = entry.Location;

                if (entry.Body.Length > 0)
                {
                    context.Response.ContentLength = entry.Body.Length;
                    await context.Response.Body.WriteAsync(entry.Body, context.RequestAborted);
                }

                return;
            }
        }

        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);

            if (context.Response.StatusCode < StatusCodes.Status500InternalServerError)
            {
                var response = new CachedResponse(
                    context.Response.StatusCode,
                    context.Response.ContentType ?? string.Empty,
                    context.Response.Headers.Location.ToString(),
                    buffer.ToArray());

                var payload = JsonSerializer.SerializeToUtf8Bytes(response, SerializerOptions);
                await _cache.SetAsync(
                    cacheKey,
                    payload,
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10) },
                    context.RequestAborted);
            }

            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody, context.RequestAborted);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private static bool ShouldApply(HttpRequest request) =>
        HttpMethods.IsPost(request.Method) || HttpMethods.IsPut(request.Method) || HttpMethods.IsDelete(request.Method);

    private sealed record CachedResponse(int StatusCode, string ContentType, string Location, byte[] Body);
}