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

    /// <summary>
    /// Field names that must bypass payload masking/redaction rules.
    /// </summary>
    public HashSet<string> MaskingExcludedFields { get; set; } = StrictMaskingFields.CreateDefaultMaskingExcludedFields();

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
        HashSet<string> normalizedExcluded = new(StringComparer.OrdinalIgnoreCase);
        foreach (string key in MaskingExcludedFields)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                normalizedExcluded.Add(key.Trim());
            }
        }

        foreach (string key in StrictMaskingFields.CreateDefaultMaskingExcludedFields())
        {
            normalizedExcluded.Add(key);
        }

        MaskingExcludedFields = normalizedExcluded;
        Rules = Rules.Where(rule => !string.IsNullOrWhiteSpace(rule.Pattern)).ToList();
    }
}
