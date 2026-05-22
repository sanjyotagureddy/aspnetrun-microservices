namespace SharedKernel.Exceptions;

internal static class ExceptionHelpers
{
    internal static string ComposeCode(int httpStatus, string errorCode, string serviceCode)
    {
        // Ensure inputs are non-null and trimmed
        var e = errorCode?.Trim() ?? "99";
        var s = serviceCode?.Trim() ?? string.Empty;
        return $"{httpStatus}_{e}-{s}";
    }
}