namespace Common.SharedKernel.Messaging;

internal static class MessageContractCompatibility
{
    public static bool IsCompatible(
        MessageContractDescriptor actual,
        MessageContractDescriptor expected,
        IReadOnlyCollection<string>? supportedVersions = null,
        string? supportedMessageType = null)
    {
        if (!string.IsNullOrWhiteSpace(supportedMessageType)
            && !string.Equals(actual.MessageType, supportedMessageType, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (supportedVersions is { Count: > 0 }
            && !supportedVersions.Contains(actual.Version, StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.Equals(actual.MessageType, expected.MessageType, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        bool actualParsed = TryParseVersion(actual.Version, out int actualMajor, out int actualMinor);
        bool expectedParsed = TryParseVersion(expected.Version, out int expectedMajor, out int expectedMinor);

        if (!actualParsed || !expectedParsed)
        {
            return string.Equals(actual.Version, expected.Version, StringComparison.OrdinalIgnoreCase);
        }

        return expected.Compatibility switch
        {
            CompatibilityMode.None => actualMajor == expectedMajor && actualMinor == expectedMinor,
            CompatibilityMode.Backward => actualMajor == expectedMajor && actualMinor <= expectedMinor,
            CompatibilityMode.Forward => actualMajor == expectedMajor && actualMinor >= expectedMinor,
            CompatibilityMode.Full => actualMajor == expectedMajor,
            _ => actualMajor == expectedMajor && actualMinor == expectedMinor
        };
    }

    private static bool TryParseVersion(string version, out int major, out int minor)
    {
        major = 0;
        minor = 0;

        if (string.IsNullOrWhiteSpace(version))
        {
            return false;
        }

        string[] parts = version.Split('.', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        if (!int.TryParse(parts[0], out major))
        {
            return false;
        }

        if (parts.Length > 1 && !int.TryParse(parts[1], out minor))
        {
            return false;
        }

        return true;
    }
}
