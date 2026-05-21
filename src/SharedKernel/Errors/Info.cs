namespace SharedKernel.Errors;

public sealed record Info
{
    public string Code { get; }

    public string Description { get; }

    public Info(string code, string description)
    {
        Code = code;
        Description = description;
    }

    public Info() : this(string.Empty, string.Empty)
    {
    }
}
