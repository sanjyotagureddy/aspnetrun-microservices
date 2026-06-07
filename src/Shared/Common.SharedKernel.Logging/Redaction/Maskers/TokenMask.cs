using System.Text.RegularExpressions;

namespace Common.SharedKernel.Logging;

internal sealed class TokenMask : IMask
{
    public string Name => "Token";

    private static readonly Regex JwtPattern = new(@"^[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex BearerPattern = new(@"^Bearer\s+.+$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    private static readonly Regex OpaqueTokenPattern = new(@"^[A-Za-z0-9+/=_\-.]{20,}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public bool TryMask(string key, object? value, string maskValue, out object? maskedValue)
    {
        maskedValue = value;
        if (value is null)
        {
            return false;
        }

        if (StrictMaskingFields.IsCredentialField(key))
        {
            maskedValue = maskValue;
            return true;
        }

        string text = value.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        if (JwtPattern.IsMatch(text) || BearerPattern.IsMatch(text) || OpaqueTokenPattern.IsMatch(text))
        {
            maskedValue = maskValue;
            return true;
        }

        return false;
    }
}
