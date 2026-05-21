namespace SharedKernel.Exceptions;

using System;
using System.Collections.Generic;
using SharedKernel;
using SharedKernel.Errors;

/// <summary>
/// Common exception types that derive from <see cref="BaseException"/>.
/// Each class composes the canonical error code string using the provided
/// `serviceCode` (padded) and a default application error code from Constants.
/// </summary>
public class NotFoundException : BaseException
{
    public NotFoundException(string serviceCode, string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null)
        : base(404, ExceptionHelpers.ComposeCode(404, Constants.CommonErrorCodes.NotFound, serviceCode), message ?? "Resource not found.", info, innerExceptions)
    {
    }

    public NotFoundException(string serviceCode, string errorCode, string message, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null)
        : base(404, ExceptionHelpers.ComposeCode(404, errorCode, serviceCode), message, info, innerExceptions)
    {
    }
}