using System.Text.RegularExpressions;

namespace Common.SharedKernel.Logging;

internal sealed class EmailMask : IMask
{
    public string Name => "Email";

    private static readonly Regex Pattern = new(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public bool TryMask(string key, object? value, string maskValue, out object? maskedValue)
    {
        maskedValue = value;
        if (value is null)
        {
            return false;
        }

        if (StrictMaskingFields.IsEmailField(key))
        {
            maskedValue = maskValue;
            return true;
        }

        string text = value.ToString() ?? string.Empty;
        if (!Pattern.IsMatch(text))
        {
            return false;
        }

        maskedValue = maskValue;
        return true;
    }
}
