using System.Text.RegularExpressions;

namespace Common.SharedKernel.Logging;

internal sealed class CreditCardMask : IMask
{
    public string Name => "CreditCard";

    private static readonly Regex Pattern = new(@"\b(?:\d[ -]*?){13,19}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public bool TryMask(string key, object? value, string maskValue, out object? maskedValue)
    {
        maskedValue = value;
        if (value is null)
        {
            return false;
        }

        if (StrictMaskingFields.IsCreditCardField(key))
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
