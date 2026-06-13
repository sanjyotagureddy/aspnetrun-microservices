namespace Common.SharedKernel.Exceptions;

public sealed class AuthenticationFailedException(string message = "Authentication is required to access this resource.")
    : BaseApplicationException(message);
