namespace Common.SharedKernel.Logging;

public static class LogTypeSemantics
{
    private static readonly IReadOnlySet<string> EmptyFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static readonly IReadOnlySet<string> CommonRequiredFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "timestampUtc",
            "serviceName",
            "message",
            "logType",
            "logCategory",
            "environment",
            "correlationId"
        };

    public static readonly IReadOnlySet<string> TraceRequiredFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "timestampUtc",
            "serviceName",
            "message",
            "logType",
            "logCategory",
            "environment",
            "correlationId"
        };

    public static readonly IReadOnlySet<string> ApiRequiredFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "timestampUtc",
            "serviceName",
            "message",
            "logType",
            "logCategory",
            "environment",
            "correlationId",
            "method",
            "path",
            "statusCode",
            "durationMs"
        };

    public static readonly IReadOnlySet<string> ErrorRequiredFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "timestampUtc",
            "serviceName",
            "message",
            "logType",
            "logCategory",
            "environment",
            "correlationId",
            "exceptionType"
        };

    public static readonly IReadOnlySet<string> TraceRecommendedFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "traceId",
            "spanId",
            "parentSpanId",
            "operation",
            "component",
            "durationMs",
            "tenantId",
            "userId",
            "hostName",
            "machineName"
        };

    public static readonly IReadOnlySet<string> ApiRecommendedFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "requestId",
            "traceId",
            "spanId",
            "routeTemplate",
            "endpoint",
            "clientIp",
            "userAgent",
            "host",
            "scheme",
            "requestSize",
            "responseSize",
            "tenantId",
            "userId",
            "success"
        };

    public static readonly IReadOnlySet<string> ErrorRecommendedFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "traceId",
            "spanId",
            "exceptionMessage",
            "errorCode",
            "category",
            "tenantId",
            "userId"
        };

    public static IReadOnlySet<string> GetRequiredFields(LogCategory logCategory)
        => logCategory switch
        {
            LogCategory.Trace => TraceRequiredFields,
            LogCategory.Api => ApiRequiredFields,
            LogCategory.Error => ErrorRequiredFields,
            _ => CommonRequiredFields
        };

    public static IReadOnlySet<string> GetRecommendedFields(LogCategory logCategory)
        => logCategory switch
        {
            LogCategory.Trace => TraceRecommendedFields,
            LogCategory.Api => ApiRecommendedFields,
            LogCategory.Error => ErrorRecommendedFields,
            _ => EmptyFields
        };
}
