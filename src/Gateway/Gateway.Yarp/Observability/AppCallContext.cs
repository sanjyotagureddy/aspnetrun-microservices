using Common.SharedKernel.Observability.Context;

namespace Gateway.Yarp.Observability;

internal sealed class AppCallContext(
    string correlationId,
    string? parentCorrelationId = null,
    string? traceId = null,
    string? spanId = null,
    string? tenantId = null,
    IDictionary<string, string>? headers = null,
    IDictionary<string, object?>? items = null)
    : AppCallContextBase(correlationId, parentCorrelationId, traceId, spanId, headers, items, "gateway-yarp")
{
    public string ServiceName { get; } = "gateway-yarp";

    public string? TenantId { get; } = string.IsNullOrWhiteSpace(tenantId) ? null : tenantId;
}
