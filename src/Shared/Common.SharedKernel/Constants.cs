namespace Common.SharedKernel;

public static class Constants
{
    public const string DefaultApiVersion = "v1";

    public static class Headers
    {
        // Tracing & Observability
        public const string CorrelationId = "x-correlationid";
        public const string RequestId = "x-request-id";
        public const string TransactionId = "x-txn-id";
        public const string TraceId = "x-traceid";
        public const string SpanId = "x-spanid";
        public const string ParentSpanId = "x-parent-spanid";
        public const string RequestStart = "x-requeststart";
        public const string ServiceName = "x-servicename";

        // Security & Identity
        public const string UserId = "x-userid";
        public const string ClientId = "x-clientid";
        public const string SessionId = "x-sessionid";
        public const string TenantId = "x-tenantid";
        public const string Roles = "x-roles";
        public const string Scope = "x-scope";
        public const string AuthMethod = "x-auth-method";
        public const string DeviceId = "x-deviceid";
        public const string AppVersion = "x-appversion";
        public const string ApiKey = "x-api-key";
        public const string UserToken = "x-user-token";

        // Performance & Debugging
        public const string Region = "x-region";
        public const string Environment = "x-environment";
        public const string ApiVersion = "x-apiversion";
        public const string FeatureFlag = "x-feature-flag";
        public const string DebugMode = "x-debug-mode";

        // ---------------------- Standard Request Headers ----------------------

        public const string UserAgent = "user-agent";
        public const string Authorization = "authorization";
        public const string ContentType = "content-type";
        public const string Accept = "accept";
        public const string AcceptEncoding = "accept-encoding";
        public const string AcceptLanguage = "accept-language";
        public const string Host = "host";
        public const string Referer = "referer";
        public const string Origin = "origin";
        public const string Connection = "connection";
        public const string CacheControl = "cache-control";
        public const string Pragma = "pragma";
        public const string XForwardedFor = "x-forwarded-for";
        public const string XForwardedProto = "x-forwarded-proto";
        public const string XForwardedPort = "x-forwarded-port";
        public const string XRequestId = "x-request-id";
        public const string XRealIp = "x-real-ip";
        public const string XApiKey = "x-api-key";
        public const string IfModifiedSince = "if-modified-since";
        public const string IfNoneMatch = "if-none-match";
        public const string Range = "range";

        public const string SetCookie = "set-cookie";
        public const string Expires = "expires";
        public const string LastModified = "last-modified";
        public const string ETag = "etag";
        public const string XPoweredBy = "x-powered-by";
        public const string AccessControlAllowOrigin = "access-control-allow-origin";
        public const string AccessControlAllowCredentials = "access-control-allow-credentials";
        public const string AccessControlAllowMethods = "access-control-allow-methods";
        public const string AccessControlAllowHeaders = "access-control-allow-headers";
        public const string XFrameOptions = "x-frame-options";
        public const string XXSSProtection = "x-xss-protection";
        public const string XContentTypeOptions = "x-content-type-options";
        public const string StrictTransportSecurity = "strict-transport-security";
        public const string ReferrerPolicy = "referrer-policy";
        public const string FeaturePolicy = "feature-policy";
        public const string ContentSecurityPolicy = "content-security-policy";
    }

}
