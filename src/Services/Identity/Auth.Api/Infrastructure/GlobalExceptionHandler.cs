using Common.SharedKernel.Exceptions;
using Common.SharedKernel.Logging;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ValidationException = Common.SharedKernel.Exceptions.ValidationException;

namespace Auth.Api.Infrastructure;

internal sealed class GlobalExceptionHandler(Common.SharedKernel.Logging.ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized, "Authentication required"),
            AuthorizationDeniedException => (StatusCodes.Status403Forbidden, "Access denied"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        await logger.LogApplicationAsync(
            new ErrorLog
            {
                Message = "Unhandled exception while processing HTTP request.",
                Category = "http.unhandled.exception",
                Exception = exception,
                ExceptionType = exception.GetType().FullName,
                ExceptionMessage = exception.Message,
                Context = new Dictionary<string, object?>
                {
                    ["operation"] = "http.request.unhandled",
                    ["method"] = httpContext.Request.Method,
                    ["path"] = httpContext.Request.Path.Value,
                    ["statusCode"] = statusCode,
                    ["traceId"] = httpContext.TraceIdentifier
                }
            },
            cancellationToken);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        ProblemDetails problemDetails = new()
        {
            Status = statusCode,
            Title = title,
            Detail = exception.Message,
            Instance = httpContext.Request.Path,
            Extensions =
            {
                ["traceId"] = httpContext.TraceIdentifier
            }
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
