namespace SharedKernel.Exceptions;

public class IdempotencyConflictException(string serviceCode, string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null) : BaseException(409, ExceptionHelpers.ComposeCode(409, Constants.CommonErrorCodes.IdempotencyConflict, serviceCode), message ?? "Idempotency conflict.", info, innerExceptions)
{
}