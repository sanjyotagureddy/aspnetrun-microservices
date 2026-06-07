namespace Common.SharedKernel.Logging;

internal sealed class DefaultMask : IMask
{
    public string Name => "Default";

    public bool TryMask(string key, object? value, string maskValue, out object? maskedValue)
    {
        maskedValue = value;
        if (value is null)
        {
            return false;
        }

        string text = value.ToString() ?? string.Empty;
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        maskedValue = MaskPreservingEnds(text);
        return true;
    }

    private static string MaskPreservingEnds(string value)
    {
        if (value.Length <= 2)
        {
            return value;
        }

        return string.Create(value.Length, value, static (buffer, source) =>
        {
            buffer[0] = source[0];
            for (int i = 1; i < source.Length - 1; i++)
            {
                buffer[i] = '*';
            }

            buffer[^1] = source[^1];
        });
    }
}