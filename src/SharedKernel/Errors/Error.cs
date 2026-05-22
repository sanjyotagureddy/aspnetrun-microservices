namespace SharedKernel.Errors;

public sealed record Error
{
    public string Code { get; }

    public string Description { get; }

    public List<Info> Info { get; }

    public Error(string code, string description, params Info[] info)
    {
        Code = code;
        Description = description;
        Info = [.. info];
    }

    public Error() : this(string.Empty, string.Empty)
    {
    }
}
