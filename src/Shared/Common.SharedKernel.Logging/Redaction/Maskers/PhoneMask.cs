using System.Text.RegularExpressions;

namespace Common.SharedKernel.Logging;

internal sealed class PhoneMask : IMask
{
    public string Name => "Phone";

    private static readonly Regex Pattern = new(@"\b(?:\+?\d{1,3}[ -]?)?(?:\(?\d{3}\)?[ -]?)\d{3}[ -]?\d{4}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public bool TryMask(string key, object? value, string maskValue, out object? maskedValue)
    {
        maskedValue = value;
        if (value is null)
        {
            return false;
        }

        if (StrictMaskingFields.IsPhoneField(key))
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
