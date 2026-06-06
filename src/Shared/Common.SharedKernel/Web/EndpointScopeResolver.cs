namespace Common.SharedKernel.Web;

[ExcludeFromCodeCoverage]
public static class EndpointScopeResolver
{
    public static EndpointScope Resolve(string? environmentName)
    {
        if (string.IsNullOrWhiteSpace(environmentName))
        {
            return EndpointScope.Development;
        }

        if (environmentName.Equals("Production", StringComparison.OrdinalIgnoreCase) ||
            environmentName.Equals("Prod", StringComparison.OrdinalIgnoreCase))
        {
            return EndpointScope.Production;
        }

        if (environmentName.Equals("Uat", StringComparison.OrdinalIgnoreCase) ||
            environmentName.Equals("Staging", StringComparison.OrdinalIgnoreCase) ||
            environmentName.Equals("Stage", StringComparison.OrdinalIgnoreCase) ||
            environmentName.Equals("Qa", StringComparison.OrdinalIgnoreCase) ||
            environmentName.Equals("Test", StringComparison.OrdinalIgnoreCase))
        {
            return EndpointScope.Uat;
        }

        return EndpointScope.Development;
    }
}
