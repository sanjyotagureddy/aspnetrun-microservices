namespace SharedKernel.Errors;

public sealed record Info(string Code, string Description)
{
    public Info() : this(string.Empty, string.Empty)
    {
    }
}
