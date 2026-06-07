namespace Common.SharedKernel.Logging;

public sealed record LoggingPolicyOptions
{
    private static readonly string[] DefaultSensitiveKeys =
    [
        "password",
        "token",
        "secret",
        "apiKey",
        "authorization",
        "cookie"
    ];

    public bool EnableRedaction { get; set; } = true;

    public bool RedactExceptionMessages { get; set; } = true;

    public HashSet<string> SensitiveKeys { get; set; } = new(DefaultSensitiveKeys, StringComparer.OrdinalIgnoreCase);

    public void EnsureDefaults()
    {
        HashSet<string> normalized = new(StringComparer.OrdinalIgnoreCase);
        foreach (string key in SensitiveKeys)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                normalized.Add(key);
            }
        }

        foreach (string key in DefaultSensitiveKeys)
        {
            normalized.Add(key);
        }

        SensitiveKeys = normalized;
    }
}