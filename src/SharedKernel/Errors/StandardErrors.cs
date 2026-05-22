#nullable enable

namespace SharedKernel.Errors;

using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// Central place for commonly used exception factories.
/// Use `Errors.ServerSide.*` and `Errors.ClientSide.*` to create pre-composed
/// exceptions that carry the structured <see cref="Error"/> payload.
/// `Errors.Common.*` remains as a compatibility wrapper around `ServerSide`.
/// </summary>
public static class Errors
{
    public static class ServerSide
    {
        public static ConfigurationMissingException ConfigurationMissing(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") =>
            new ConfigurationMissingException(ExceptionHelpers.ResolveServiceCode(callerFilePath), message, info, innerExceptions);

        public static ValidationException Validation(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") =>
            new ValidationException(ExceptionHelpers.ResolveServiceCode(callerFilePath), message, info, innerExceptions);

        public static NotFoundException NotFound(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") =>
            new NotFoundException(ExceptionHelpers.ResolveServiceCode(callerFilePath), message, info, innerExceptions);

        public static ConflictException Conflict(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") =>
            new ConflictException(ExceptionHelpers.ResolveServiceCode(callerFilePath), message, info, innerExceptions);

        public static UnauthorizedException Unauthorized(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") =>
            new UnauthorizedException(ExceptionHelpers.ResolveServiceCode(callerFilePath), message, info, innerExceptions);

        public static ForbiddenException Forbidden(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") =>
            new ForbiddenException(ExceptionHelpers.ResolveServiceCode(callerFilePath), message, info, innerExceptions);

        public static DependencyFailureException DependencyFailure(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") =>
            new DependencyFailureException(ExceptionHelpers.ResolveServiceCode(callerFilePath), message, info, innerExceptions);

        public static IdempotencyConflictException IdempotencyConflict(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") =>
            new IdempotencyConflictException(ExceptionHelpers.ResolveServiceCode(callerFilePath), message, info, innerExceptions);

        public static BaseException Unknown(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") =>
            new BaseException(500, ExceptionHelpers.ComposeCode(500, Constants.CommonErrorCodes.Unknown, ExceptionHelpers.ResolveServiceCode(callerFilePath)), message ?? "Unknown error.", info, innerExceptions);
    }

    public static class ClientSide
    {
        public static ValidationException Validation(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") => ServerSide.Validation(message, info, innerExceptions, callerFilePath);

        public static NotFoundException NotFound(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") => ServerSide.NotFound(message, info, innerExceptions, callerFilePath);

        public static ConflictException Conflict(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") => ServerSide.Conflict(message, info, innerExceptions, callerFilePath);

        public static UnauthorizedException Unauthorized(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") => ServerSide.Unauthorized(message, info, innerExceptions, callerFilePath);

        public static ForbiddenException Forbidden(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") => ServerSide.Forbidden(message, info, innerExceptions, callerFilePath);

        public static IdempotencyConflictException IdempotencyConflict(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") => ServerSide.IdempotencyConflict(message, info, innerExceptions, callerFilePath);
    }

    // Keep Common as a compatibility wrapper that forwards to ServerSide
    public static class Common
    {
        public static ConfigurationMissingException ConfigurationMissing(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") => ServerSide.ConfigurationMissing(message, info, innerExceptions, callerFilePath);

        public static ValidationException Validation(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") => ServerSide.Validation(message, info, innerExceptions, callerFilePath);

        public static NotFoundException NotFound(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") => ServerSide.NotFound(message, info, innerExceptions, callerFilePath);

        public static ConflictException Conflict(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") => ServerSide.Conflict(message, info, innerExceptions, callerFilePath);

        public static UnauthorizedException Unauthorized(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") => ServerSide.Unauthorized(message, info, innerExceptions, callerFilePath);

        public static ForbiddenException Forbidden(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") => ServerSide.Forbidden(message, info, innerExceptions, callerFilePath);

        public static DependencyFailureException DependencyFailure(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") => ServerSide.DependencyFailure(message, info, innerExceptions, callerFilePath);

        public static IdempotencyConflictException IdempotencyConflict(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") => ServerSide.IdempotencyConflict(message, info, innerExceptions, callerFilePath);

        public static BaseException Unknown(string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null, [CallerFilePath] string callerFilePath = "") => ServerSide.Unknown(message, info, innerExceptions, callerFilePath);
    }

    public static class UseCase
    {
        // Place for domain/use-case specific exception factories.
        // Example:
        // public static ProductOutOfStockException ProductOutOfStock(string serviceCode) =>
        //     new ProductOutOfStockException(serviceCode);
    }
}
