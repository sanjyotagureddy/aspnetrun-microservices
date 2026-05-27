namespace Common.SharedKernel.Logging;

internal sealed class CorrelationEnricher : ILogEnricher
{
    public void Enrich(LogEnrichmentContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.Context?.CorrelationId))
        {
            context.SetProperty("correlationId", context.Context.CorrelationId);
        }
    }
}

internal sealed class TraceEnricher : ILogEnricher
{
    public void Enrich(LogEnrichmentContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.Context?.TraceId))
        {
            context.SetProperty("traceId", context.Context.TraceId);
        }

        if (!string.IsNullOrWhiteSpace(context.Context?.SpanId))
        {
            context.SetProperty("spanId", context.Context.SpanId);
        }
    }
}

internal sealed class EnvironmentEnricher : ILogEnricher
{
    public void Enrich(LogEnrichmentContext context)
    {
        context.SetProperty("environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production");
        context.SetProperty("machineName", Environment.MachineName);
        context.SetProperty("processId", Environment.ProcessId);
    }
}

internal sealed class MachineEnricher : ILogEnricher
{
    public void Enrich(LogEnrichmentContext context)
    {
        context.SetProperty("machineName", Environment.MachineName);
    }
}

internal sealed class TenantEnricher : ILogEnricher
{
    public void Enrich(LogEnrichmentContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.Context?.TenantId))
        {
            context.SetProperty("tenantId", context.Context.TenantId);
        }
    }
}

internal sealed class UserEnricher : ILogEnricher
{
    public void Enrich(LogEnrichmentContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.Context?.UserId))
        {
            context.SetProperty("userId", context.Context.UserId);
        }
    }
}
