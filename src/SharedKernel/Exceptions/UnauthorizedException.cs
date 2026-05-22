namespace SharedKernel.Exceptions;

public class UnauthorizedException(string serviceCode, string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null)
    : BaseException(401, ExceptionHelpers.ComposeCode(401, Constants.CommonErrorCodes.Unauthorized, serviceCode), message ?? "Unauthorized.", info, innerExceptions)
{
}
