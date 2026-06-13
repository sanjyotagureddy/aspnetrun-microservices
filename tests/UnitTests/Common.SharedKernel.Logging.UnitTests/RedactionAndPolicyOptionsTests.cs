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
        options.MaskingExcludedFields.Should().Contain("correlationid");
        options.MaskingExcludedFields.Should().Contain("traceparent");
        options.MaskingExcludedFields.Should().NotContain("productid");
    }

    [Fact]
    public void DefaultLogRedactor_Redact_ShouldBypassMasking_ForConfiguredExcludedField()
    {
        LoggingPolicyOptions policy = new()
        {
            SensitiveKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "tenantId"
            },
            MaskingExcludedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "tenantId"
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
                ["tenantId"] = "tenant-01",
                ["authorization"] = "Bearer abc.def.ghi"
            });

        LogEntry redacted = redactor.Redact(entry);

        redacted.Properties.Should().NotBeNull();
        redacted.Properties!["tenantId"].Should().Be("tenant-01");
        redacted.Properties["authorization"].Should().Be("***");
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

    [Fact]
    public void DefaultLogRedactor_Redact_ShouldMaskHeaderStyleSensitiveKeys()
    {
        LoggingPolicyOptions policy = new();
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
                ["rq.authorization"] = "Bearer token-value",
                ["rs.content-type"] = "application/json"
            });

        LogEntry redacted = redactor.Redact(entry);

        redacted.Properties.Should().NotBeNull();
        redacted.Properties!["rq.authorization"].Should().Be("***");
        redacted.Properties["rs.content-type"].Should().Be("application/json");
    }

    [Fact]
    public void DefaultLogRedactor_Redact_ShouldMaskCardAndPhoneValues()
    {
        LoggingPolicyOptions policy = new();
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
                ["customerPhone"] = "+1 415-555-1212",
                ["card"] = "4111 1111 1111 1111"
            });

        LogEntry redacted = redactor.Redact(entry);

        redacted.Properties.Should().NotBeNull();
        redacted.Properties!["customerPhone"].Should().Be("***");
        redacted.Properties["card"].Should().Be("***");
    }

    [Fact]
    public void DefaultLogRedactor_Redact_ShouldMaskEmailAndTokenValues()
    {
        LoggingPolicyOptions policy = new();
        policy.EnsureDefaults();

        DefaultLogRedactor redactor = new(Options.Create(policy), [new EmailMask(), new TokenMask()]);
        LogEntry entry = LogEntry.Create(
            LogLevel.Information,
            "Catalog",
            "Catalog.Api",
            "request",
            "message",
            DateTimeOffset.UtcNow,
            properties: new Dictionary<string, object?>
            {
                ["contactEmail"] = "user@example.com",
                ["rq.authorization"] = "Bearer abc.def.ghi",
                ["sku"] = "ABC-1"
            });

        LogEntry redacted = redactor.Redact(entry);

        redacted.Properties.Should().NotBeNull();
        redacted.Properties!["contactEmail"].Should().Be("***");
        redacted.Properties["rq.authorization"].Should().Be("***");
        redacted.Properties["sku"].Should().Be("ABC-1");
    }

    [Fact]
    public void DefaultLogRedactor_Redact_ShouldUseConfiguredFieldToMaskerMapping()
    {
        LoggingPolicyOptions policy = new() { SensitiveKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) };
        policy.EnsureDefaults();

        MaskingOptions masking = new()
        {
            FieldMaskers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["customerEmail"] = "Email"
            }
        };

        DefaultLogRedactor redactor = new(
            Options.Create(policy),
            [new EmailMask(), new PhoneMask(), new DefaultMask()],
            Options.Create(masking));

        LogEntry entry = LogEntry.Create(
            LogLevel.Information,
            "Catalog",
            "Catalog.Api",
            "request",
            "message",
            DateTimeOffset.UtcNow,
            properties: new Dictionary<string, object?>
            {
                ["customerEmail"] = "user@example.com",
                ["customerPhone"] = "+1 415-555-1212"
            });

        LogEntry redacted = redactor.Redact(entry);

        redacted.Properties.Should().NotBeNull();
        redacted.Properties!["customerEmail"].Should().Be("***");
        string maskedPhone = redacted.Properties["customerPhone"]!.ToString()!;
        maskedPhone.Should().HaveLength("+1 415-555-1212".Length);
        maskedPhone[0].Should().Be('+');
        maskedPhone[^1].Should().Be('2');
        maskedPhone.Should().Contain("*");
    }

    [Fact]
    public void CreditCardMask_ShouldMaskStrictField_EvenWhenValueDoesNotMatchPattern()
    {
        CreditCardMask mask = new();

        bool masked = mask.TryMask("request.cardNumber", "encrypted-value", "***", out object? maskedValue);

        masked.Should().BeTrue();
        maskedValue.Should().Be("***");
    }

    [Fact]
    public void TokenMask_ShouldMaskStrictCredentialField_EvenWhenValueDoesNotMatchPattern()
    {
        TokenMask mask = new();

        bool masked = mask.TryMask("rq.authorization", "abc", "***", out object? maskedValue);

        masked.Should().BeTrue();
        maskedValue.Should().Be("***");
    }

    [Fact]
    public void DefaultLogRedactor_Redact_ShouldPreserveCorrelationTraceAndTraceparent()
    {
        LoggingPolicyOptions policy = new()
        {
            MaskingExcludedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "topic",
                "messageId",
                "eventType",
                "productId"
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
                ["correlationId"] = "7f2aa1b3-8b7d-4d2b-9f80-28dd7c2d9a4f",
                ["traceId"] = "0af7651916cd43dd8448eb211c80319c",
                ["rq.traceparent"] = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01",
                ["rs.x-correlationid"] = "7f2aa1b3-8b7d-4d2b-9f80-28dd7c2d9a4f",
                ["messaging.topic"] = "inventory.stock.updated",
                ["messaging.messageId"] = "d8b3d4b8-a8aa-43ea-97b4-f3a8f5f1d06f",
                ["eventType"] = "product.created.v1",
                ["productId"] = "d8b3d4b8-a8aa-43ea-97b4-f3a8f5f1d06f",
                ["authorization"] = "Bearer abc.def.ghi"
            });

        LogEntry redacted = redactor.Redact(entry);

        redacted.Properties.Should().NotBeNull();
        redacted.Properties!["correlationId"].Should().Be("7f2aa1b3-8b7d-4d2b-9f80-28dd7c2d9a4f");
        redacted.Properties["traceId"].Should().Be("0af7651916cd43dd8448eb211c80319c");
        redacted.Properties["rq.traceparent"].Should().Be("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
        redacted.Properties["rs.x-correlationid"].Should().Be("7f2aa1b3-8b7d-4d2b-9f80-28dd7c2d9a4f");
        redacted.Properties["messaging.topic"].Should().Be("inventory.stock.updated");
        redacted.Properties["messaging.messageId"].Should().Be("d8b3d4b8-a8aa-43ea-97b4-f3a8f5f1d06f");
        redacted.Properties["eventType"].Should().Be("product.created.v1");
        redacted.Properties["productId"].Should().Be("d8b3d4b8-a8aa-43ea-97b4-f3a8f5f1d06f");
        redacted.Properties["authorization"].Should().Be("***");
    }
}
