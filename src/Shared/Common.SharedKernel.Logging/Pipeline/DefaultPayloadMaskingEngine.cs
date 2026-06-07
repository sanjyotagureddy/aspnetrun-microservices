using System.Security.Cryptography;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text.Json.Nodes;

namespace Common.SharedKernel.Logging;

internal sealed class DefaultPayloadMaskingEngine(IReadOnlyList<IMask> maskers) : IPayloadMaskingEngine
{
    private readonly IReadOnlyList<IMask> _maskers = maskers;
    private readonly MaskingOptions _maskingOptions = new();

    public DefaultPayloadMaskingEngine() : this([
        new CreditCardMask(),
        new PhoneMask(),
        new EmailMask(),
        new TokenMask(),
        new DefaultMask()])
    {
    }

    public DefaultPayloadMaskingEngine(IReadOnlyList<IMask> maskers, IOptions<MaskingOptions> maskingOptions) : this(maskers)
    {
        _maskingOptions = maskingOptions.Value;
    }

    private static readonly JsonSerializerOptions SerializationOptions = new(JsonSerializerDefaults.Web)
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    public PayloadProtectionResult Apply(PayloadProtectionRequest request, PayloadProtectionOptions options)
    {
        if (request.Payload is null)
        {
            return new PayloadProtectionResult(true, null, 0, 0);
        }

        string serialized = SerializePayload(request.Payload);
        if (Encoding.UTF8.GetByteCount(serialized) > options.MaxPayloadSizeBytes)
        {
            return new PayloadProtectionResult(
                false,
                null,
                0,
                0,
                new PayloadProtectionFailure(
                    "payload_too_large",
                    "Payload exceeds configured protection size limit.",
                    options.FailureBehavior));
        }

        JsonNode? root = JsonNode.Parse(serialized);
        if (root is null)
        {
            return new PayloadProtectionResult(true, null, 0, 0);
        }

        HashSet<string> sensitiveKeys = new(options.GlobalSensitiveFields, StringComparer.OrdinalIgnoreCase);
        if (request.GlobalSensitiveFields is not null)
        {
            foreach (string key in request.GlobalSensitiveFields)
            {
                if (!string.IsNullOrWhiteSpace(key))
                {
                    sensitiveKeys.Add(key.Trim());
                }
            }
        }

        IReadOnlyCollection<PayloadRule> rules = request.Rules ?? options.Rules;

        int maskedCount = 0;
        int redactedCount = 0;
        Traverse(root, "$", 0, options, sensitiveKeys, rules, ref maskedCount, ref redactedCount);

        return new PayloadProtectionResult(true, root.ToJsonString(), maskedCount, redactedCount);
    }

    private void Traverse(
        JsonNode node,
        string path,
        int depth,
        PayloadProtectionOptions options,
        IReadOnlySet<string> sensitiveKeys,
        IReadOnlyCollection<PayloadRule> rules,
        ref int maskedCount,
        ref int redactedCount)
    {
        if (depth >= options.MaxRecursionDepth)
        {
            return;
        }

        if (node is JsonObject obj)
        {
            foreach (KeyValuePair<string, JsonNode?> kvp in obj.ToList())
            {
                string key = kvp.Key;
                string childPath = $"{path}.{key}";

                PayloadRuleAction? action = ResolveAction(key, childPath, sensitiveKeys, rules);
                if (action is not null)
                {
                    ApplyAction(obj, key, kvp.Value, action.Value, options.MaskValue, ref maskedCount, ref redactedCount);
                    continue;
                }

                if (kvp.Value is JsonValue && TryMaskValueByPattern(obj, key, kvp.Value, options.MaskValue, _maskers, _maskingOptions))
                {
                    maskedCount++;
                    continue;
                }

                if (kvp.Value is not null)
                {
                    Traverse(kvp.Value, childPath, depth + 1, options, sensitiveKeys, rules, ref maskedCount, ref redactedCount);
                }
            }
            return;
        }

        if (node is JsonArray array)
        {
            for (int i = 0; i < array.Count; i++)
            {
                JsonNode? child = array[i];
                if (child is null)
                {
                    continue;
                }

                Traverse(child, $"{path}[{i}]", depth + 1, options, sensitiveKeys, rules, ref maskedCount, ref redactedCount);
            }
        }
    }

    private static PayloadRuleAction? ResolveAction(
        string key,
        string path,
        IReadOnlySet<string> sensitiveKeys,
        IReadOnlyCollection<PayloadRule> rules)
    {
        if (StrictMaskingFields.IsObservabilityIdentityField(key)
            || StrictMaskingFields.IsObservabilityIdentityField(GetTerminalKey(key)))
        {
            return null;
        }

        foreach (PayloadRule rule in rules)
        {
            if (rule.MatchType is PayloadRuleMatchType.GlobalField
                && (string.Equals(rule.Pattern, key, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(rule.Pattern, GetTerminalKey(key), StringComparison.OrdinalIgnoreCase)))
            {
                return rule.Action;
            }

            if (rule.MatchType is PayloadRuleMatchType.Path && PathMatches(rule.Pattern, path))
            {
                return rule.Action;
            }
        }

        if (sensitiveKeys.Contains(key) || sensitiveKeys.Contains(GetTerminalKey(key)))
        {
            return PayloadRuleAction.Mask;
        }

        return null;
    }

    private static string GetTerminalKey(string key)
    {
        int dotIndex = key.LastIndexOf('.');
        return dotIndex >= 0 ? key[(dotIndex + 1)..] : key;
    }

    private static bool PathMatches(string pattern, string path)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return false;
        }

        string normalizedPattern = pattern.StartsWith("$", StringComparison.Ordinal) ? pattern : $"$.{pattern}";

        string regexPattern = "^" + Regex.Escape(normalizedPattern)
            .Replace("\\[\\*\\]", "\\[\\d+\\]")
            .Replace("\\*", ".*") + "$";

        return Regex.IsMatch(path, regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static string SerializePayload(object payload)
    {
        if (payload is string text)
        {
            return text;
        }

        return JsonSerializer.Serialize(payload, SerializationOptions);
    }

    private static void ApplyAction(
        JsonObject parent,
        string key,
        JsonNode? value,
        PayloadRuleAction action,
        string maskValue,
        ref int maskedCount,
        ref int redactedCount)
    {
        switch (action)
        {
            case PayloadRuleAction.Remove:
                parent.Remove(key);
                redactedCount++;
                break;
            case PayloadRuleAction.Hash:
                parent[key] = HashValue(value);
                maskedCount++;
                break;
            case PayloadRuleAction.PartialMask:
                parent[key] = PartialMaskValue(value, maskValue);
                maskedCount++;
                break;
            case PayloadRuleAction.Custom:
            case PayloadRuleAction.Mask:
            default:
                parent[key] = maskValue;
                maskedCount++;
                break;
        }
    }

    private static string PartialMaskValue(JsonNode? value, string maskValue)
    {
        string raw = value is null ? string.Empty : value.ToJsonString().Trim('"');
        if (raw.Length <= 4)
        {
            return maskValue;
        }

        string suffix = raw[^4..];
        return $"{maskValue}{suffix}";
    }

    private static string HashValue(JsonNode? value)
    {
        string raw = value?.ToJsonString() ?? string.Empty;
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static bool TryMaskValueByPattern(
        JsonObject parent,
        string key,
        JsonNode? value,
        string maskValue,
        IReadOnlyList<IMask> maskers,
        MaskingOptions maskingOptions)
    {
        if (StrictMaskingFields.IsObservabilityIdentityField(key)
            || StrictMaskingFields.IsObservabilityIdentityField(GetTerminalKey(key)))
        {
            return false;
        }

        if (value is null)
        {
            return false;
        }

        string text = value.ToJsonString().Trim('"');
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        IEnumerable<IMask> candidates = ResolveMaskersForKey(key, maskers, maskingOptions);
        foreach (IMask masker in candidates)
        {
            if (masker.TryMask(key, text, maskValue, out object? maskedValue))
            {
                parent[key] = maskedValue is null
                    ? null
                    : JsonSerializer.SerializeToNode(maskedValue);
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<IMask> ResolveMaskersForKey(string key, IReadOnlyList<IMask> maskers, MaskingOptions options)
    {
        if (options.FieldMaskers.Count is 0)
        {
            return maskers.Where(masker => !string.Equals(masker.Name, "Default", StringComparison.OrdinalIgnoreCase));
        }

        HashSet<string> configured = new(StringComparer.OrdinalIgnoreCase);

        foreach ((string fieldPattern, string maskerName) in options.FieldMaskers)
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
            IMask? defaultMasker = maskers.FirstOrDefault(masker => string.Equals(masker.Name, "Default", StringComparison.OrdinalIgnoreCase));
            return defaultMasker is null ? [] : [defaultMasker];
        }

        List<IMask> selected = maskers.Where(masker => configured.Contains(masker.Name)).ToList();
        if (selected.Count is 0)
        {
            IMask? defaultMasker = maskers.FirstOrDefault(masker => string.Equals(masker.Name, "Default", StringComparison.OrdinalIgnoreCase));
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
