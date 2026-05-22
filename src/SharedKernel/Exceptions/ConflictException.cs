using SharedKernel.Errors;

namespace SharedKernel.Exceptions;

public class ConflictException : BaseException
{
    public ConflictException(string serviceCode, string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null)
        : base(409, ExceptionHelpers.ComposeCode(409, Constants.CommonErrorCodes.Conflict, serviceCode), message ?? "Conflict.", info, innerExceptions)
    {
    }

    public ConflictException(string serviceCode, string errorCode, string message, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null)
        : base(409, ExceptionHelpers.ComposeCode(409, errorCode, serviceCode), message, info, innerExceptions)
    {
    }
}