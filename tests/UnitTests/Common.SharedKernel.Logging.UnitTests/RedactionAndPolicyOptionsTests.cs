using Microsoft.Extensions.Options;

namespace Common.SharedKernel.Logging.UnitTests;

public sealed class RedactionAndPolicyOptionsTests
{
    [Fact]
    public void LoggingPolicyOptions_EnsureDefaults_ShouldAddBaselineKeys()
    {
        LoggingPolicyOptions options = new()
        {
            SensitiveKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "customerEmail",
                ""
            }
        };

        options.EnsureDefaults();

        options.SensitiveKeys.Should().Contain("customerEmail");
        options.SensitiveKeys.Should().Contain("password");
        options.SensitiveKeys.Should().Contain("authorization");
        options.SensitiveKeys.Should().NotContain("");
    }

    [Fact]
    public void DefaultLogRedactor_Redact_ShouldMaskConfiguredSensitiveProperties()
    {
        LoggingPolicyOptions policy = new()
        {
            SensitiveKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "apiKey"
            }
        };
        policy.EnsureDefaults();

        DefaultLogRedactor redactor = new(Options.Create(policy));
        LogEntry entry = LogEntry.Create(
            LogLevel.Information,
            "Catalog",
            "Catalog.Api",
            "request",
            "message",
            DateTimeOffset.UtcNow,
            properties: new Dictionary<string, object?>
            {
                ["apiKey"] = "secret-value",
                ["sku"] = "ABC-1"
            });

        LogEntry redacted = redactor.Redact(entry);

        redacted.Properties.Should().NotBeNull();
        redacted.Properties!["apiKey"].Should().Be("***");
        redacted.Properties["sku"].Should().Be("ABC-1");
    }

    [Fact]
    public void DefaultLogRedactor_Redact_ShouldReturnSameEntry_WhenDisabled()
    {
        LoggingPolicyOptions policy = new() { EnableRedaction = false };
        policy.EnsureDefaults();

        DefaultLogRedactor redactor = new(Options.Create(policy));
        LogEntry entry = LogEntry.Create(
            LogLevel.Information,
            "Catalog",
            "Catalog.Api",
            "request",
            "message",
            DateTimeOffset.UtcNow,
            properties: new Dictionary<string, object?>
            {
                ["password"] = "p@ss"
            });

        LogEntry redacted = redactor.Redact(entry);

        redacted.Should().BeSameAs(entry);
    }
}