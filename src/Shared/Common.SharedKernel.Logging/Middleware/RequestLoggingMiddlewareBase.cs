using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Common.SharedKernel.Logging;

public abstract class RequestLoggingMiddlewareBase(
    RequestDelegate next,
    ILogger<RequestLoggingMiddlewareBase> logger,
    ILogContextAccessor contextAccessor,
    IOptions<LoggingMiddlewareOptions> middlewareOptions,
    IPayloadProtectionPipeline payloadProtectionPipeline,
    IPayloadStore payloadStore)
{
    private static readonly IReadOnlySet<string> AllowedHeaderNames = BuildAllowedHeaderNames();

    public async Task InvokeAsync(HttpContext httpContext)
    {
        LoggingMiddlewareOptions options = middlewareOptions.Value;
        var requestPath = httpContext.Request.Path.Value ?? string.Empty;
        if (ShouldSkip(requestPath, options.ExcludedRoutes))
        {
            await next(httpContext).ConfigureAwait(false);
            return;
        }

        Activity? activity = Activity.Current;

        LogContext logContext = BuildLogContext(httpContext, activity);
        using IDisposable scope = contextAccessor.BeginScope(logContext);

        string? requestPayloadCandidate = null;
        if (options.CaptureRequestPayloadCandidates
            && ShouldCapturePayloadByRoute(requestPath, options)
            && IsPayloadContentTypeAllowed(httpContext.Request.ContentType, options.AllowedPayloadContentTypes)
            && IsContentLengthAllowed(httpContext.Request.ContentLength, options.MaxPayloadCaptureBytes)
            && CanHaveBody(httpContext.Request.Method))
        {
            requestPayloadCandidate = await ReadRequestPayloadAsync(
                httpContext,
                options.MaxPayloadCaptureBytes,
                httpContext.RequestAborted).ConfigureAwait(false);
        }

        Stream originalResponseBody = httpContext.Response.Body;
        MemoryStream? responseBuffer = null;
        if (options.CaptureResponsePayloadCandidates && ShouldCapturePayloadByRoute(requestPath, options))
        {
            responseBuffer = new MemoryStream();
            httpContext.Response.Body = responseBuffer;
        }

        var startedTimestamp = Stopwatch.GetTimestamp();
        Exception? capturedException = null;

        try
        {
            await next(httpContext).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            capturedException = exception;
            throw;
        }
        finally
        {
            string? responsePayloadCandidate = null;
            if (responseBuffer is not null)
            {
                responsePayloadCandidate = await ReadResponsePayloadAsync(
                    responseBuffer,
                    httpContext.Response.ContentType,
                    options,
                    httpContext.RequestAborted).ConfigureAwait(false);

                responseBuffer.Position = 0;
                await responseBuffer.CopyToAsync(originalResponseBody, httpContext.RequestAborted).ConfigureAwait(false);
                httpContext.Response.Body = originalResponseBody;
                await responseBuffer.DisposeAsync().ConfigureAwait(false);
            }

            TimeSpan duration = Stopwatch.GetElapsedTime(startedTimestamp);
            var routeTemplate = ResolveRouteTemplate(httpContext);
            Dictionary<string, object?> completionProperties = new(StringComparer.OrdinalIgnoreCase)
            {
                ["method"] = httpContext.Request.Method,
                ["path"] = requestPath,
                ["routeTemplate"] = routeTemplate,
                ["endpoint"] = requestPath,
                ["url"] = BuildRequestUrl(httpContext),
                ["statusCode"] = httpContext.Response.StatusCode,
                ["durationMs"] = duration.TotalMilliseconds,
                ["traceId"] = activity?.TraceId.ToString(),
                ["spanId"] = activity?.SpanId.ToString()
            };

            await AddProtectedHeadersAsync(completionProperties, "rq", httpContext.Request.Headers, payloadProtectionPipeline, httpContext)
                .ConfigureAwait(false);
            await AddProtectedHeadersAsync(completionProperties, "rs", httpContext.Response.Headers, payloadProtectionPipeline, httpContext)
                .ConfigureAwait(false);

            if (capturedException is null)
            {
                completionProperties["logType"] = "app";

                if (!string.IsNullOrWhiteSpace(requestPayloadCandidate))
                {
                    PayloadProtectionResult requestProtectionResult = await ProtectPayloadAsync(
                        payloadProtectionPipeline,
                        requestPayloadCandidate,
                        "http.request.payload",
                        httpContext).ConfigureAwait(false);
                    JsonNode? requestPayloadNode = AddPayloadProtectionProperties(completionProperties, "request", requestProtectionResult);
                    if (requestPayloadNode is not null)
                    {
                        await AddSegmentPayloadReferenceAsync(completionProperties, "request", requestPayloadNode, payloadStore, httpContext)
                            .ConfigureAwait(false);
                    }
                }

                if (!string.IsNullOrWhiteSpace(responsePayloadCandidate))
                {
                    PayloadProtectionResult responseProtectionResult = await ProtectPayloadAsync(
                        payloadProtectionPipeline,
                        responsePayloadCandidate,
                        "http.response.payload",
                        httpContext).ConfigureAwait(false);
                    JsonNode? responsePayloadNode = AddPayloadProtectionProperties(completionProperties, "response", responseProtectionResult);
                    if (responsePayloadNode is not null)
                    {
                        await AddSegmentPayloadReferenceAsync(completionProperties, "response", responsePayloadNode, payloadStore, httpContext)
                            .ConfigureAwait(false);
                    }
                }

                EnrichCompletionProperties(httpContext, completionProperties, capturedException);

                await logger.LogApiAsync(
                    new ApiLog
                    {
                        Message = "HTTP request completed",
                        Category = "app_request",
                        Method = httpContext.Request.Method,
                        Path = requestPath,
                        RouteTemplate = routeTemplate,
                        Url = BuildRequestUrl(httpContext),
                        StatusCode = httpContext.Response.StatusCode,
                        DurationMs = duration.TotalMilliseconds,
                        Context = completionProperties
                    },
                    LogType.Application,
                    CancellationToken.None).ConfigureAwait(false);
            }
            else
            {
                completionProperties["logType"] = "app";
                completionProperties["exceptionType"] = capturedException.GetType().FullName;
                completionProperties["exceptionMessage"] = capturedException.Message;

                if (!string.IsNullOrWhiteSpace(requestPayloadCandidate))
                {
                    PayloadProtectionResult requestProtectionResult = await ProtectPayloadAsync(
                        payloadProtectionPipeline,
                        requestPayloadCandidate,
                        "http.request.payload",
                        httpContext).ConfigureAwait(false);
                    JsonNode? requestPayloadNode = AddPayloadProtectionProperties(completionProperties, "request", requestProtectionResult);
                    if (requestPayloadNode is not null)
                    {
                        await AddSegmentPayloadReferenceAsync(completionProperties, "request", requestPayloadNode, payloadStore, httpContext)
                            .ConfigureAwait(false);
                    }
                }

                if (!string.IsNullOrWhiteSpace(responsePayloadCandidate))
                {
                    PayloadProtectionResult responseProtectionResult = await ProtectPayloadAsync(
                        payloadProtectionPipeline,
                        responsePayloadCandidate,
                        "http.response.payload",
                        httpContext).ConfigureAwait(false);
                    JsonNode? responsePayloadNode = AddPayloadProtectionProperties(completionProperties, "response", responseProtectionResult);
                    if (responsePayloadNode is not null)
                    {
                        await AddSegmentPayloadReferenceAsync(completionProperties, "response", responsePayloadNode, payloadStore, httpContext)
                            .ConfigureAwait(false);
                    }
                }

                if (options.CaptureExceptionPayloadCandidates)
                {
                    PayloadProtectionResult exceptionProtectionResult = await ProtectPayloadAsync(
                        payloadProtectionPipeline,
                        new
                        {
                            type = capturedException.GetType().FullName,
                            message = capturedException.Message,
                            stackTrace = capturedException.StackTrace
                        },
                        "http.exception.payload",
                        httpContext).ConfigureAwait(false);

                    JsonNode? exceptionPayloadNode = AddPayloadProtectionProperties(completionProperties, "exception", exceptionProtectionResult);
                    if (exceptionPayloadNode is not null)
                    {
                        await AddSegmentPayloadReferenceAsync(completionProperties, "exception", exceptionPayloadNode, payloadStore, httpContext)
                            .ConfigureAwait(false);
                    }
                }

                EnrichCompletionProperties(httpContext, completionProperties, capturedException);

                await logger.LogErrorAsync(
                    new ErrorLog
                    {
                        Message = "HTTP request failed",
                        Category = "http.request.failed",
                        Exception = capturedException,
                        ExceptionType = capturedException.GetType().FullName,
                        ExceptionMessage = capturedException.Message,
                        Context = completionProperties
                    },
                    LogType.Application,
                    CancellationToken.None).ConfigureAwait(false);
            }
        }
    }

    protected virtual void EnrichCompletionProperties(HttpContext httpContext, IDictionary<string, object?> completionProperties, Exception? exception)
    {
    }

    private static bool ShouldSkip(string path, IReadOnlySet<string> excludedRoutes)
    {
        if (excludedRoutes.Count is 0)
        {
            return false;
        }

        foreach (var route in excludedRoutes)
        {
            if (string.IsNullOrWhiteSpace(route))
            {
                continue;
            }

            if (path.StartsWith(route, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ShouldCapturePayloadByRoute(string path, LoggingMiddlewareOptions options)
    {
        return !ShouldSkip(path, options.ExcludedPayloadRoutes);
    }

    private static bool IsPayloadContentTypeAllowed(string? contentType, IReadOnlySet<string> allowedContentTypes)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return false;
        }

        var normalized = contentType.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0];
        if (allowedContentTypes.Contains(normalized))
        {
            return true;
        }

        return normalized.EndsWith("+json", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsContentLengthAllowed(long? contentLength, int maxPayloadCaptureBytes)
    {
        return contentLength is null || (contentLength >= 0 && contentLength <= maxPayloadCaptureBytes);
    }

    private static bool CanHaveBody(string method)
    {
        return !HttpMethods.IsGet(method)
               && !HttpMethods.IsHead(method)
               && !HttpMethods.IsDelete(method)
               && !HttpMethods.IsTrace(method);
    }

    private static async Task<string?> ReadRequestPayloadAsync(HttpContext httpContext, int maxPayloadCaptureBytes, CancellationToken cancellationToken)
    {
        HttpRequest request = httpContext.Request;
        request.EnableBuffering();

        if (request.Body.CanSeek)
        {
            request.Body.Position = 0;
        }

        using MemoryStream buffer = new();
        await request.Body.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);

        if (request.Body.CanSeek)
        {
            request.Body.Position = 0;
        }

        if (buffer.Length is 0 || buffer.Length > maxPayloadCaptureBytes)
        {
            return null;
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static async Task<string?> ReadResponsePayloadAsync(
        MemoryStream responseBuffer,
        string? contentType,
        LoggingMiddlewareOptions options,
        CancellationToken cancellationToken)
    {
        if (!IsPayloadContentTypeAllowed(contentType, options.AllowedPayloadContentTypes))
        {
            return null;
        }

        if (responseBuffer.Length is 0 || responseBuffer.Length > options.MaxPayloadCaptureBytes)
        {
            return null;
        }

        responseBuffer.Position = 0;
        using MemoryStream snapshot = new();
        await responseBuffer.CopyToAsync(snapshot, cancellationToken).ConfigureAwait(false);
        responseBuffer.Position = 0;

        return Encoding.UTF8.GetString(snapshot.ToArray());
    }

    private static async Task<PayloadProtectionResult> ProtectPayloadAsync(
        IPayloadProtectionPipeline payloadProtectionPipeline,
        object payload,
        string source,
        HttpContext httpContext)
    {
        PayloadProtectionRequest protectionRequest = new(
            payload,
            source,
            ContentType: "application/json",
            CorrelationId: ReadHeader(httpContext, Constants.Headers.CorrelationId),
            TraceId: Activity.Current?.TraceId.ToString());

        return await payloadProtectionPipeline.ProtectAsync(protectionRequest, httpContext.RequestAborted).ConfigureAwait(false);
    }

    private static JsonNode? AddPayloadProtectionProperties(
        IDictionary<string, object?> target,
        string prefix,
        PayloadProtectionResult result)
    {
        target[$"{prefix}.protection"] = result.Success;
        target[$"{prefix}.mask.count"] = result.MaskedFieldCount;
        target[$"{prefix}.redact.count"] = result.RedactedFieldCount;

        if (result.Success && result.ProtectedPayload is not null)
        {
            if (TryParseProtectedPayloadNode(result.ProtectedPayload, out JsonNode? payloadNode))
            {
                return payloadNode;
            }

            target[$"{prefix}.protection.failure.code"] = "protected_payload_parse_failed";
        }

        if (result.Failure is not null)
        {
            target[$"{prefix}.protection.failure.code"] = result.Failure.Code;
            target[$"{prefix}.protection.failure.behavior"] = result.Failure.Behavior.ToString();
        }

        return null;
    }

    private static async Task AddSegmentPayloadReferenceAsync(
        IDictionary<string, object?> target,
        string segment,
        JsonNode payload,
        IPayloadStore payloadStore,
        HttpContext httpContext)
    {
        try
        {
            PayloadStoreWriteResult storeResult = await payloadStore.StoreAsync(
                new PayloadStoreWriteRequest(
                    payload,
                    "application/json",
                    CorrelationId: ReadHeader(httpContext, Constants.Headers.CorrelationId),
                    TraceId: Activity.Current?.TraceId.ToString()),
                httpContext.RequestAborted).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(storeResult.PayloadRef))
            {
                target[$"{segment}.url"] = storeResult.PayloadRef;
            }

            if (!string.IsNullOrWhiteSpace(storeResult.PayloadHash))
            {
                target[$"{segment}.hash"] = storeResult.PayloadHash;
            }

            target[$"{segment}.size"] = storeResult.PayloadSizeBytes;
            target[$"{segment}.encoding"] = storeResult.PayloadEncoding;
            target[$"{segment}.compressed"] = storeResult.Compressed;
            target[$"{segment}.encrypted"] = storeResult.Encrypted;
        }
        catch
        {
            target[$"{segment}.store.failure"] = "store_failed";
        }
    }

    private static async Task AddProtectedHeadersAsync(
        IDictionary<string, object?> target,
        string fieldPrefix,
        IHeaderDictionary headers,
        IPayloadProtectionPipeline payloadProtectionPipeline,
        HttpContext httpContext)
    {
        Dictionary<string, string> candidates = ExtractHeaders(headers);
        if (candidates.Count is 0)
        {
            return;
        }

        PayloadProtectionResult result = await ProtectPayloadAsync(
            payloadProtectionPipeline,
            candidates,
            $"http.{fieldPrefix}.headers",
            httpContext).ConfigureAwait(false);

        if (!result.Success || result.ProtectedPayload is null)
        {
            target[$"{fieldPrefix}.headers.failure"] = "header_masking_failed";
            return;
        }

        if (!TryParseProtectedPayloadObject(result.ProtectedPayload, out JsonObject? payloadObject) || payloadObject is null)
        {
            target[$"{fieldPrefix}.headers.failure"] = "header_parse_failed";
            return;
        }

        foreach (KeyValuePair<string, JsonNode?> header in payloadObject)
        {
            var headerKey = NormalizeHeaderName(header.Key);
            target[$"{fieldPrefix}.{headerKey}"] = header.Value is null
                ? null
                : header.Value is JsonValue jsonValue && jsonValue.TryGetValue(out string? text)
                    ? text
                    : header.Value.ToJsonString();
        }
    }

    private static Dictionary<string, string> ExtractHeaders(IHeaderDictionary headers)
    {
        Dictionary<string, string> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> header in headers)
        {
            if (Microsoft.Extensions.Primitives.StringValues.IsNullOrEmpty(header.Value))
            {
                continue;
            }

            var normalizedHeaderName = NormalizeHeaderName(header.Key);
            if (!AllowedHeaderNames.Contains(normalizedHeaderName))
            {
                continue;
            }

            result[normalizedHeaderName] = header.Value.ToString();
        }

        return result;
    }

    private static IReadOnlySet<string> BuildAllowedHeaderNames()
    {
        HashSet<string> allowed = new(StringComparer.OrdinalIgnoreCase);

        foreach (FieldInfo field in typeof(Constants.Headers).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType != typeof(string))
            {
                continue;
            }

            if (field.GetValue(null) is string headerName && !string.IsNullOrWhiteSpace(headerName))
            {
                allowed.Add(NormalizeHeaderName(headerName));
            }
        }

        return allowed;
    }

    private static bool TryParseProtectedPayloadObject(object protectedPayload, out JsonObject? payloadObject)
    {
        payloadObject = null;
        switch (protectedPayload)
        {
            case JsonObject jsonObject:
                payloadObject = jsonObject;
                return true;
            case string text:
            {
                var parsed = JsonNode.Parse(text);
                payloadObject = parsed as JsonObject;
                return payloadObject is not null;
            }
        }

        JsonNode? serialized = JsonSerializer.SerializeToNode(protectedPayload);
        payloadObject = serialized as JsonObject;
        return payloadObject is not null;
    }

    private static bool TryParseProtectedPayloadNode(object protectedPayload, out JsonNode? payloadNode)
    {
        payloadNode = null;
        switch (protectedPayload)
        {
            case JsonNode:
                payloadNode = (JsonNode)protectedPayload;
                return true;
            case string text:
                payloadNode = JsonNode.Parse(text);
                return payloadNode is not null;
            default:
                payloadNode = JsonSerializer.SerializeToNode(protectedPayload);
                return payloadNode is not null;
        }
    }

    private static string NormalizeHeaderName(string headerName)
    {
        return headerName.Trim().ToLowerInvariant();
    }

    private static string BuildRequestUrl(HttpContext httpContext)
    {
        return $"{httpContext.Request.Scheme}://{httpContext.Request.Host}{httpContext.Request.Path}{httpContext.Request.QueryString}";
    }

    private static LogContext BuildLogContext(HttpContext httpContext, Activity? activity)
    {
        var correlationId = ReadHeader(httpContext, Constants.Headers.CorrelationId)
            ?? activity?.TraceId.ToString()
            ?? httpContext.TraceIdentifier;

        var tenantId = ReadHeader(httpContext, Constants.Headers.TenantId);
        var userId = ReadHeader(httpContext, Constants.Headers.UserId)
            ?? httpContext.User.Identity?.Name;

        return new LogContext
        {
            CorrelationId = correlationId,
            RequestId = httpContext.TraceIdentifier,
            TraceId = activity?.TraceId.ToString(),
            SpanId = activity?.SpanId.ToString(),
            TenantId = tenantId,
            UserId = userId
        };
    }

    private static string ResolveRouteTemplate(HttpContext httpContext)
    {
        Endpoint? endpoint = httpContext.GetEndpoint();
        switch (endpoint)
        {
            case RouteEndpoint routeEndpoint:
            {
                var template = routeEndpoint.RoutePattern.RawText ?? httpContext.Request.Path.Value ?? string.Empty;
                return NormalizeRouteTemplate(template);
            }
            default:
                return endpoint?.DisplayName ?? httpContext.Request.Path.Value ?? string.Empty;
        }
    }

    private static string NormalizeRouteTemplate(string template)
    {
        return string.IsNullOrWhiteSpace(template) ? string.Empty :
            Regex.Replace(template, @"\{([^}:]+):[^}]+\}", "{$1}", RegexOptions.CultureInvariant, new TimeSpan(0, 0, 0, 0, 100));
    }

    private static string? ReadHeader(HttpContext httpContext, string key)
    {
        if (httpContext.Request.Headers.TryGetValue(key, out Microsoft.Extensions.Primitives.StringValues value)
            && !Microsoft.Extensions.Primitives.StringValues.IsNullOrEmpty(value))
        {
            return value.ToString();
        }

        return null;
    }
}
