namespace Common.SharedKernel.Exceptions;

public sealed class ValidationException : BaseApplicationException
{
    public string ParamName { get; }

    public ValidationException(string paramName, string message)
        : base(message)
    {
        ParamName = paramName;
    }
    public ValidationException(string message)
        : base(message)
    {
    }
}
