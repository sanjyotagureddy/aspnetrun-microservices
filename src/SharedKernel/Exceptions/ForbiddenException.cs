namespace SharedKernel.Exceptions;

public class ForbiddenException(string serviceCode, string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null) : BaseException(403, ExceptionHelpers.ComposeCode(403, Constants.CommonErrorCodes.Forbidden, serviceCode), message ?? "Forbidden.", info, innerExceptions)
{
}