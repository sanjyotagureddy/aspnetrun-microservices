namespace Common.SharedKernel.Web;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EndpointScopeAttribute : Attribute
{
    private readonly HashSet<EndpointScope> _allowedScopes;

    public EndpointScopeAttribute(params EndpointScope[] scopes)
    {
        ArgumentNullException.ThrowIfNull(scopes);

        if (scopes.Length == 0)
        {
            throw new ArgumentException("At least one endpoint scope must be specified.", nameof(scopes));
        }

        this._allowedScopes = scopes.ToHashSet();
    }

    public bool Includes(EndpointScope scope)
    {
        return this._allowedScopes.Contains(scope);
    }
}