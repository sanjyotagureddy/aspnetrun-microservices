using SharedKernel.Errors;

namespace SharedKernel.Exceptions;

public class DependencyFailureException(string serviceCode, string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null) : BaseException(502, ExceptionHelpers.ComposeCode(502, Constants.CommonErrorCodes.DependencyFailure, serviceCode), message ?? "Dependency failure.", info, innerExceptions)
{
}