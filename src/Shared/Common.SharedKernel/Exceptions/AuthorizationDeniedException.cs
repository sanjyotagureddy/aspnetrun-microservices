namespace Common.SharedKernel.Exceptions;

public sealed class AuthorizationDeniedException(string message = "You do not have permission to access this resource.")
    : BaseApplicationException(message);
