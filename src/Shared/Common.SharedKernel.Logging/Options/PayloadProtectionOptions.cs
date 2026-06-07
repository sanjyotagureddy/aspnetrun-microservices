namespace Common.SharedKernel.Logging;

public sealed record PayloadProtectionOptions
{
    public bool Enabled { get; set; } = true;

    public int MaxRecursionDepth { get; set; } = 32;

    public int MaxPayloadSizeBytes { get; set; } = 262144;

    public PayloadProtectionFailureBehavior FailureBehavior { get; set; } = PayloadProtectionFailureBehavior.PersistMetadataOnly;

    public string MaskValue { get; set; } = "***MASKED***";

    public HashSet<string> GlobalSensitiveFields { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "email",
        "password",
        "cardNumber",
        "creditCard",
        "creditCardNumber",
        "pan",
        "cvv",
        "phone",
        "phoneNumber",
        "ssn",
        "accountNumber",
        "accessToken",
        "refreshToken",
        "authorization",
        "apiKey",
        "secret"
    };

    public List<PayloadRule> Rules { get; set; } = [];

    public void EnsureDefaults()
    {
        if (MaxRecursionDepth <= 0)
        {
            MaxRecursionDepth = 32;
        }

        if (MaxPayloadSizeBytes <= 0)
        {
            MaxPayloadSizeBytes = 262144;
        }

        HashSet<string> normalized = new(StringComparer.OrdinalIgnoreCase);
        foreach (string key in GlobalSensitiveFields)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                normalized.Add(key.Trim());
            }
        }

        GlobalSensitiveFields = normalized;
        Rules = Rules.Where(rule => !string.IsNullOrWhiteSpace(rule.Pattern)).ToList();
    }
}
