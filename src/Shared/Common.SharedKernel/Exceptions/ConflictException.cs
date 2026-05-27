namespace Common.SharedKernel.Exceptions;

public sealed class ConflictException(string message) : BaseApplicationException(message);