using System.Dynamic;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace Common.SharedKernel.Logging.UnitTests;

public sealed class PayloadProtectionPipelineTests
{
    [Fact]
    public void DefaultPayloadMaskingEngine_ShouldMaskNestedSensitiveFields()
    {
        DefaultPayloadMaskingEngine engine = new();
        PayloadProtectionOptions options = new();
        options.EnsureDefaults();

        var payload = new
        {
            customer = new
            {
                email = "test@example.com",
                profile = new
                {
                    password = "p@ssw0rd"
                }
            },
            sku = "ABC-1"
        };

        PayloadProtectionResult result = engine.Apply(new PayloadProtectionRequest(payload, "unit-test"), options);

        result.Success.Should().BeTrue();
        result.MaskedFieldCount.Should().Be(2);

        string protectedPayload = result.ProtectedPayload.Should().BeOfType<string>().Subject;
        JsonNode? root = JsonNode.Parse(protectedPayload);
        root!["customer"]!["email"]!.GetValue<string>().Should().Be(options.MaskValue);
        root["customer"]!["profile"]!["password"]!.GetValue<string>().Should().Be(options.MaskValue);
        root["sku"]!.GetValue<string>().Should().Be("ABC-1");
    }

    [Fact]
    public void DefaultPayloadMaskingEngine_ShouldApplyWildcardPathRules_ForArrays()
    {
        DefaultPayloadMaskingEngine engine = new();
        PayloadProtectionOptions options = new();
        options.EnsureDefaults();

        List<PayloadRule> rules =
        [
            new("$.orders[*].cardNumber", PayloadRuleMatchType.Path, PayloadRuleAction.Hash),
            new("$.orders[*].cvv", PayloadRuleMatchType.Path, PayloadRuleAction.Remove)
        ];

        var payload = new
        {
            orders = new[]
            {
                new { cardNumber = "4111111111111111", cvv = "123" },
                new { cardNumber = "5555555555554444", cvv = "456" }
            }
        };

        PayloadProtectionResult result = engine.Apply(new PayloadProtectionRequest(payload, "unit-test", Rules: rules), options);

        result.Success.Should().BeTrue();
        result.MaskedFieldCount.Should().Be(2);
        result.RedactedFieldCount.Should().Be(2);

        string protectedPayload = result.ProtectedPayload.Should().BeOfType<string>().Subject;
        JsonNode? root = JsonNode.Parse(protectedPayload);
        JsonArray orders = root!["orders"]!.AsArray();
        orders[0]!["cardNumber"]!.GetValue<string>().Should().HaveLength(64);
        orders[1]!["cardNumber"]!.GetValue<string>().Should().HaveLength(64);
        orders[0]!["cvv"].Should().BeNull();
        orders[1]!["cvv"].Should().BeNull();
    }

    [Fact]
    public void DefaultPayloadMaskingEngine_ShouldApplyAllRuleActions()
    {
        DefaultPayloadMaskingEngine engine = new();
        PayloadProtectionOptions options = new();
        options.EnsureDefaults();

        List<PayloadRule> rules =
        [
            new("phone", PayloadRuleMatchType.GlobalField, PayloadRuleAction.PartialMask),
            new("token", PayloadRuleMatchType.GlobalField, PayloadRuleAction.Hash),
            new("secret", PayloadRuleMatchType.GlobalField, PayloadRuleAction.Remove),
            new("name", PayloadRuleMatchType.GlobalField, PayloadRuleAction.Custom)
        ];

        var payload = new
        {
            phone = "1234567890",
            token = "token-abc",
            secret = "secret-value",
            name = "John"
        };

        PayloadProtectionResult result = engine.Apply(new PayloadProtectionRequest(payload, "unit-test", Rules: rules), options);

        result.Success.Should().BeTrue();
        result.MaskedFieldCount.Should().Be(3);
        result.RedactedFieldCount.Should().Be(1);

        string protectedPayload = result.ProtectedPayload.Should().BeOfType<string>().Subject;
        JsonNode? root = JsonNode.Parse(protectedPayload);
        root!["phone"]!.GetValue<string>().Should().EndWith("7890");
        root["token"]!.GetValue<string>().Should().HaveLength(64);
        root["secret"].Should().BeNull();
        root["name"]!.GetValue<string>().Should().Be(options.MaskValue);
    }

    [Fact]
    public void DefaultPayloadMaskingEngine_ShouldSupportExpandoObjectPayloads()
    {
        DefaultPayloadMaskingEngine engine = new();
        PayloadProtectionOptions options = new();
        options.EnsureDefaults();

        dynamic payload = new ExpandoObject();
        payload.apiKey = "my-api-key";
        payload.customer = new ExpandoObject();
        payload.customer.password = "secret";

        PayloadProtectionResult result = engine.Apply(new PayloadProtectionRequest(payload, "unit-test"), options);

        result.Success.Should().BeTrue();
        result.MaskedFieldCount.Should().Be(2);

        string protectedPayload = result.ProtectedPayload.Should().BeOfType<string>().Subject;
        JsonNode? root = JsonNode.Parse(protectedPayload);
        root!["apiKey"]!.GetValue<string>().Should().Be(options.MaskValue);
        root["customer"]!["password"]!.GetValue<string>().Should().Be(options.MaskValue);
    }

    [Fact]
    public void DefaultPayloadMaskingEngine_ShouldHandleCircularReferencesSafely()
    {
        DefaultPayloadMaskingEngine engine = new();
        PayloadProtectionOptions options = new();
        options.EnsureDefaults();

        CircularNode first = new() { Name = "first", Password = "p1" };
        CircularNode second = new() { Name = "second", Password = "p2" };
        first.Next = second;
        second.Next = first;

        first.Name.Should().Be("first");
        first.Password.Should().Be("p1");
        first.Next.Should().BeSameAs(second);
        second.Next.Should().BeSameAs(first);

        PayloadProtectionResult result = engine.Apply(new PayloadProtectionRequest(first, "unit-test"), options);

        result.Success.Should().BeTrue();
        result.MaskedFieldCount.Should().BeGreaterThan(0);
        result.Failure.Should().BeNull();
    }

    [Fact]
    public async Task PayloadProtectionPipeline_ShouldReturnFailure_WhenPayloadExceedsMaxSize()
    {
        PayloadProtectionOptions options = new()
        {
            MaxPayloadSizeBytes = 8,
            FailureBehavior = PayloadProtectionFailureBehavior.DropPayload
        };
        options.EnsureDefaults();

        PayloadProtectionPipeline pipeline = new(new DefaultPayloadMaskingEngine(), Options.Create(options));

        PayloadProtectionResult result = await pipeline.ProtectAsync(
            new PayloadProtectionRequest(new { email = "test@example.com" }, "unit-test"),
            TestContext.Current.CancellationToken);

        result.Success.Should().BeFalse();
        result.ProtectedPayload.Should().BeNull();
        result.Failure.Should().NotBeNull();
        result.Failure!.Code.Should().Be("payload_too_large");
        result.Failure.Behavior.Should().Be(PayloadProtectionFailureBehavior.DropPayload);
    }

    [Fact]
    public void DefaultPayloadMaskingEngine_ShouldMaskPhoneAndCardPatterns()
    {
        DefaultPayloadMaskingEngine engine = new();
        PayloadProtectionOptions options = new();
        options.EnsureDefaults();

        var payload = new
        {
            contact = new
            {
                phone = "+1 415-555-1212",
                note = "call me"
            },
            payment = new
            {
                freeText = "my card is 4111 1111 1111 1111"
            }
        };

        PayloadProtectionResult result = engine.Apply(new PayloadProtectionRequest(payload, "unit-test"), options);

        result.Success.Should().BeTrue();
        result.MaskedFieldCount.Should().BeGreaterThan(1);

        string protectedPayload = result.ProtectedPayload.Should().BeOfType<string>().Subject;
        JsonNode? root = JsonNode.Parse(protectedPayload);
        root!["contact"]!["phone"]!.GetValue<string>().Should().Be(options.MaskValue);
        root["payment"]!["freeText"]!.GetValue<string>().Should().Be(options.MaskValue);
        root["contact"]!["note"]!.GetValue<string>().Should().Be("call me");
    }

    [Fact]
    public void DefaultPayloadMaskingEngine_ShouldUseDefaultMaskerForUnmappedFields_WhenFieldMaskersConfigured()
    {
        MaskingOptions masking = new()
        {
            FieldMaskers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["email"] = "Email"
            }
        };

        DefaultPayloadMaskingEngine engine = new(
            [new EmailMask(), new DefaultMask()],
            Options.Create(masking));

        PayloadProtectionOptions options = new();
        options.EnsureDefaults();

        var payload = new
        {
            email = "user@example.com",
            note = "sensitive"
        };

        PayloadProtectionResult result = engine.Apply(new PayloadProtectionRequest(payload, "unit-test"), options);

        result.Success.Should().BeTrue();

        string protectedPayload = result.ProtectedPayload.Should().BeOfType<string>().Subject;
        JsonNode? root = JsonNode.Parse(protectedPayload);
        root!["email"]!.GetValue<string>().Should().Be(options.MaskValue);
        root["note"]!.GetValue<string>().Should().Be("s*******e");
    }

    [Fact]
    public void DefaultPayloadMaskingEngine_ShouldPreserveCorrelationTraceAndTraceparentFields()
    {
        DefaultPayloadMaskingEngine engine = new();
        PayloadProtectionOptions options = new()
        {
            MaskingExcludedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "topic",
                "messageId",
                "eventType",
                "productId"
            }
        };
        options.EnsureDefaults();

        var payload = new
        {
            correlationId = "7f2aa1b3-8b7d-4d2b-9f80-28dd7c2d9a4f",
            xCorrelationId = "7f2aa1b3-8b7d-4d2b-9f80-28dd7c2d9a4f",
            traceId = "0af7651916cd43dd8448eb211c80319c",
            traceparent = "00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01",
            topic = "inventory.stock.updated",
            messageId = "d8b3d4b8-a8aa-43ea-97b4-f3a8f5f1d06f",
            eventType = "product.created.v1",
            productId = "d8b3d4b8-a8aa-43ea-97b4-f3a8f5f1d06f",
            authorization = "Bearer abc.def.ghi"
        };

        PayloadProtectionResult result = engine.Apply(new PayloadProtectionRequest(payload, "unit-test"), options);

        result.Success.Should().BeTrue();

        string protectedPayload = result.ProtectedPayload.Should().BeOfType<string>().Subject;
        JsonNode? root = JsonNode.Parse(protectedPayload);
        root!["correlationId"]!.GetValue<string>().Should().Be("7f2aa1b3-8b7d-4d2b-9f80-28dd7c2d9a4f");
        root["xCorrelationId"]!.GetValue<string>().Should().Be("7f2aa1b3-8b7d-4d2b-9f80-28dd7c2d9a4f");
        root["traceId"]!.GetValue<string>().Should().Be("0af7651916cd43dd8448eb211c80319c");
        root["traceparent"]!.GetValue<string>().Should().Be("00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01");
        root["topic"]!.GetValue<string>().Should().Be("inventory.stock.updated");
        root["messageId"]!.GetValue<string>().Should().Be("d8b3d4b8-a8aa-43ea-97b4-f3a8f5f1d06f");
        root["eventType"]!.GetValue<string>().Should().Be("product.created.v1");
        root["productId"]!.GetValue<string>().Should().Be("d8b3d4b8-a8aa-43ea-97b4-f3a8f5f1d06f");
        root["authorization"]!.GetValue<string>().Should().Be(options.MaskValue);
    }

    [Fact]
    public void DefaultPayloadMaskingEngine_ShouldBypassMasking_ForConfiguredExcludedField()
    {
        DefaultPayloadMaskingEngine engine = new();
        PayloadProtectionOptions options = new()
        {
            GlobalSensitiveFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "tenantId",
                "authorization"
            },
            MaskingExcludedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "tenantId"
            }
        };
        options.EnsureDefaults();

        var payload = new
        {
            tenantId = "tenant-01",
            authorization = "Bearer abc.def.ghi"
        };

        PayloadProtectionResult result = engine.Apply(new PayloadProtectionRequest(payload, "unit-test"), options);

        result.Success.Should().BeTrue();

        string protectedPayload = result.ProtectedPayload.Should().BeOfType<string>().Subject;
        JsonNode? root = JsonNode.Parse(protectedPayload);
        root!["tenantId"]!.GetValue<string>().Should().Be("tenant-01");
        root["authorization"]!.GetValue<string>().Should().Be(options.MaskValue);
    }

    private sealed class CircularNode
    {
        public string? Name { get; init; }

        public string? Password { get; init; }

        public CircularNode? Next { get; set; }
    }
}
