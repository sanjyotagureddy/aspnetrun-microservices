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
            "level",
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
            "level",
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
            "level",
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
            "level",
            "environment",
            "correlationId",
            "exceptionType"
        };

    public static readonly IReadOnlySet<string> EventRequiredFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "timestampUtc",
            "serviceName",
            "message",
            "logType",
            "level",
            "environment",
            "correlationId",
            "eventName",
            "eventVersion"
        };

    public static readonly IReadOnlySet<string> AuditRequiredFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "timestampUtc",
            "serviceName",
            "message",
            "logType",
            "level",
            "environment",
            "correlationId",
            "action",
            "resourceType",
            "resourceId",
            "performedBy"
        };

    public static readonly IReadOnlySet<string> SecurityRequiredFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "timestampUtc",
            "serviceName",
            "message",
            "logType",
            "level",
            "environment",
            "correlationId",
            "action",
            "result"
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
            "stackTrace",
            "errorCode",
            "eventCode",
            "severity",
            "category",
            "source",
            "innerException",
            "tenantId",
            "userId"
        };

    public static readonly IReadOnlySet<string> EventRecommendedFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "eventId",
            "aggregateId",
            "aggregateType",
            "causationId",
            "topic",
            "partition",
            "offset",
            "tenantId",
            "traceId",
            "spanId"
        };

    public static readonly IReadOnlySet<string> AuditRecommendedFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "traceId",
            "spanId",
            "tenantId",
            "ipAddress",
            "userAgent",
            "result",
            "beforeStatePayloadRef",
            "afterStatePayloadRef"
        };

    public static readonly IReadOnlySet<string> SecurityRecommendedFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "traceId",
            "spanId",
            "userId",
            "tenantId",
            "ipAddress",
            "userAgent",
            "resource",
            "reason",
            "riskLevel"
        };

    public static IReadOnlySet<string> GetRequiredFields(LogType logType)
        => logType switch
        {
            LogType.Trace => TraceRequiredFields,
            LogType.Api => ApiRequiredFields,
            LogType.Error => ErrorRequiredFields,
            LogType.Event => EventRequiredFields,
            LogType.Audit => AuditRequiredFields,
            LogType.Security => SecurityRequiredFields,
            _ => CommonRequiredFields
        };

    public static IReadOnlySet<string> GetRecommendedFields(LogType logType)
        => logType switch
        {
            LogType.Trace => TraceRecommendedFields,
            LogType.Api => ApiRecommendedFields,
            LogType.Error => ErrorRecommendedFields,
            LogType.Event => EventRecommendedFields,
            LogType.Audit => AuditRecommendedFields,
            LogType.Security => SecurityRecommendedFields,
            _ => EmptyFields
        };
}
