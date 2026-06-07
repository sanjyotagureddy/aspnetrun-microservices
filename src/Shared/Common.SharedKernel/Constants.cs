namespace Common.SharedKernel;

public static class Constants
{
    public const string DefaultApiVersion = "v1";

    public static class Headers
    {
        // Tracing & Correlation
        public const string CorrelationId = "x-correlationid";
        public const string RequestId = "x-request-id";
        public const string TransactionId = "x-txn-id";
        public const string TraceId = "x-traceid";
        public const string SpanId = "x-spanid";
        public const string ParentSpanId = "x-parent-spanid";

        // Multi-Tenancy & Identity
        public const string TenantId = "x-tenantid";
        public const string UserId = "x-userid";
        public const string ClientId = "x-clientid";
        public const string SessionId = "x-sessionid";

        // Service Context
        public const string CallerService = "x-caller-service";
        public const string ServiceName = "x-servicename";
        public const string Environment = "x-environment";
        public const string Region = "x-region";
        public const string ApiVersion = "x-apiversion";

        // Network & Request Context
        
        public const string XRealIp = "x-real-ip";
        public const string Host = "host";
        public const string Origin = "origin";

        // Content & Auth
        public const string Authorization = "authorization";
        public const string ContentType = "content-type";
        public const string Accept = "accept";
    }
}