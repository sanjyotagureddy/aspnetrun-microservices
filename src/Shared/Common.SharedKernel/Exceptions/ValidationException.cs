namespace Common.SharedKernel.Exceptions;

public sealed class ValidationException(string message) : BaseApplicationException(message);
