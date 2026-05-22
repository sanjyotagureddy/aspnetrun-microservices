namespace SharedKernel.Exceptions;

/// <summary>
/// Base exception type for services. Carries an HTTP status, a composed error code
/// (format: &lt;http&gt;_&lt;error&gt;-&lt;service&gt;) and a reusable <see cref="Error"/> payload.
/// </summary>
public class BaseException : Exception
{
    public int HttpStatus { get; }

    /// <summary>
    /// Composed error code string in the canonical format: &lt;http&gt;_&lt;error&gt;-&lt;service&gt;.
    /// This must be provided by the caller.
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// Structured error payload that can be serialized to the API response.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// Create a BaseException with a pre-composed error code string. Accepts multiple inner exceptions;
    /// these will be aggregated and converted to `Info` entries appended to the provided `info` list.
    /// </summary>
    public BaseException(int httpStatus, string composedErrorCode, string message, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null)
        : base(message, innerExceptions is null ? null : new AggregateException(innerExceptions))
    {
        HttpStatus = httpStatus;
        ErrorCode = composedErrorCode ?? string.Empty;

        var infos = new List<Info>();
        if (info != null)
            infos.AddRange(info);

        if (innerExceptions != null)
        {
            foreach (Exception ex in innerExceptions)
            {
                if (ex is not null)
                {
                    // Use Unknown code for inner exceptions unless the inner is a BaseException
                    var code = ex is BaseException be ? be.ErrorCode : SharedKernel.Constants.CommonErrorCodes.Unknown;
                    infos.Add(new Info(code, ex.Message ?? string.Empty));
                }
            }
        }

        Error = new Error(ErrorCode, message, infos.ToArray());
    }
}
