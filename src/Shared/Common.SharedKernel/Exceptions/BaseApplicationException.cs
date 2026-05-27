namespace Common.SharedKernel.Exceptions;

public abstract class BaseApplicationException : Exception
{
    protected BaseApplicationException()
    {
    }

    protected BaseApplicationException(string message)
        : base(message)
    {
    }

    protected BaseApplicationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}