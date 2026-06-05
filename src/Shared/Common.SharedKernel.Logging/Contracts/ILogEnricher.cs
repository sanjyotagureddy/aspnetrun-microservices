namespace Common.SharedKernel.Logging;

public interface ILogEnricher
{
    void Enrich(LogEnrichmentContext context);
}
