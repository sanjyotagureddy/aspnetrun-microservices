using SharedKernel.Errors;

namespace SharedKernel.Exceptions;

public class ValidationException : BaseException
{
    public ValidationException(string serviceCode, string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null)
        : base(400, ExceptionHelpers.ComposeCode(400, Constants.CommonErrorCodes.Validation, serviceCode), message ?? "Validation failed.", info, innerExceptions)
    {
    }

    public ValidationException(string serviceCode, string errorCode, string message, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null)
        : base(400, ExceptionHelpers.ComposeCode(400, errorCode, serviceCode), message, info, innerExceptions)
    {
    }
}