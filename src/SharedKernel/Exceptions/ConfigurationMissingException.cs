namespace SharedKernel.Exceptions;

public class ConfigurationMissingException(string serviceCode, string? message = null, IEnumerable<Info>? info = null, IEnumerable<Exception>? innerExceptions = null) : BaseException(500, ExceptionHelpers.ComposeCode(500, Constants.CommonErrorCodes.ConfigurationMissing, serviceCode), message ?? "Configuration missing.", info, innerExceptions)
{
}