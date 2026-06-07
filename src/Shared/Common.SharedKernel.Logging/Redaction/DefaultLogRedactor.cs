using System.Text.RegularExpressions;

namespace Common.SharedKernel.Logging;

internal sealed class DefaultLogRedactor(IOptions<LoggingPolicyOptions> policyOptions) : ILogRedactor
{
    private const string RedactedValue = "***";
    private readonly IReadOnlyList<IMask> _maskers =
    [
        new CreditCardMask(),
        new PhoneMask(),
        new EmailMask(),
        new TokenMask(),
        new DefaultMask()
    ];
    private readonly MaskingOptions _maskingOptions = new();

    public DefaultLogRedactor(IOptions<LoggingPolicyOptions> policyOptions, IReadOnlyList<IMask> maskers) : this(policyOptions)
    {
        _maskers = maskers;
    }

    public DefaultLogRedactor(
        IOptions<LoggingPolicyOptions> policyOptions,
        IReadOnlyList<IMask> maskers,
        IOptions<MaskingOptions> maskingOptions) : this(policyOptions, maskers)
    {
        _maskingOptions = maskingOptions.Value;
    }

    public LogEntry Redact(LogEntry entry)
    {
        LoggingPolicyOptions policy = policyOptions.Value;
        if (!policy.EnableRedaction)
        {
            return entry;
        }

        if (entry.Properties is null || entry.Properties.Count is 0)
        {
            return entry;
        }

        Dictionary<string, object?> redactedProperties = new(StringComparer.OrdinalIgnoreCase);
        bool hasChanges = false;

        foreach (KeyValuePair<string, object?> property in entry.Properties)
        {
            string terminalKey = GetTerminalKey(property.Key);
            if (StrictMaskingFields.IsObservabilityIdentityField(property.Key)
                || StrictMaskingFields.IsObservabilityIdentityField(terminalKey))
            {
                redactedProperties[property.Key] = property.Value;
                continue;
            }

            bool maskedByStrategy = TryMaskByStrategies(property.Key, property.Value, out object? strategyMaskedValue);
            if (policy.SensitiveKeys.Contains(property.Key)
                || policy.SensitiveKeys.Contains(terminalKey)
                || maskedByStrategy)
            {
                redactedProperties[property.Key] = strategyMaskedValue ?? RedactedValue;
                hasChanges = true;
                continue;
            }

            redactedProperties[property.Key] = property.Value;
        }

        if (!hasChanges)
        {
            return entry;
        }

        return LogEntry.Create(
            entry.Level,
            entry.ServiceName,
            entry.Namespace,
            entry.Category,
            entry.Message,
            entry.TimestampUtc,
            entry.CorrelationId,
            entry.Exception,
            redactedProperties);
    }

    private static string GetTerminalKey(string key)
    {
        int dotIndex = key.LastIndexOf('.');
        return dotIndex >= 0 ? key[(dotIndex + 1)..] : key;
    }

    private bool TryMaskByStrategies(string key, object? value, out object? maskedValue)
    {
        IEnumerable<IMask> candidates = ResolveMaskersForKey(key);
        foreach (IMask masker in candidates)
        {
            if (masker.TryMask(key, value, RedactedValue, out maskedValue))
            {
                return true;
            }
        }

        maskedValue = null;
        return false;
    }

    private IEnumerable<IMask> ResolveMaskersForKey(string key)
    {
        if (_maskingOptions.FieldMaskers.Count is 0)
        {
            return _maskers.Where(masker => !string.Equals(masker.Name, "Default", StringComparison.OrdinalIgnoreCase));
        }

        HashSet<string> configured = new(StringComparer.OrdinalIgnoreCase);

        foreach ((string fieldPattern, string maskerName) in _maskingOptions.FieldMaskers)
        {
            if (string.IsNullOrWhiteSpace(fieldPattern) || string.IsNullOrWhiteSpace(maskerName))
            {
                continue;
            }

            if (MatchesFieldPattern(key, fieldPattern))
            {
                configured.Add(maskerName.Trim());
            }
        }

        if (configured.Count is 0)
        {
            IMask? defaultMasker = _maskers.FirstOrDefault(masker => string.Equals(masker.Name, "Default", StringComparison.OrdinalIgnoreCase));
            return defaultMasker is null ? [] : [defaultMasker];
        }

        List<IMask> selected = _maskers.Where(masker => configured.Contains(masker.Name)).ToList();
        if (selected.Count is 0)
        {
            IMask? defaultMasker = _maskers.FirstOrDefault(masker => string.Equals(masker.Name, "Default", StringComparison.OrdinalIgnoreCase));
            return defaultMasker is null ? [] : [defaultMasker];
        }

        return selected;
    }

    private static bool MatchesFieldPattern(string key, string pattern)
    {
        if (string.Equals(key, pattern, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string terminal = GetTerminalKey(key);
        if (string.Equals(terminal, pattern, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!pattern.Contains('*'))
        {
            return false;
        }

        string regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
        return Regex.IsMatch(key, regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
               || Regex.IsMatch(terminal, regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}