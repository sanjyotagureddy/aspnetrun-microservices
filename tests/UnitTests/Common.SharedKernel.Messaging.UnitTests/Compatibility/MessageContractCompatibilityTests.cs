namespace Common.SharedKernel.Messaging.UnitTests.Compatibility;

public sealed class MessageContractCompatibilityTests
{
    [Fact]
    public void IsCompatible_ShouldReturnTrue_ForBackwardCompatibleVersion()
    {
        MessageContractDescriptor actual = new("ProductLifecycleEvent", "1.0", "application/json");
        MessageContractDescriptor expected = new("ProductLifecycleEvent", "1.2", "application/json", Compatibility: CompatibilityMode.Backward);

        bool compatible = MessageContractCompatibility.IsCompatible(actual, expected);

        compatible.Should().BeTrue();
    }

    [Fact]
    public void IsCompatible_ShouldReturnFalse_ForDifferentMajorVersion()
    {
        MessageContractDescriptor actual = new("ProductLifecycleEvent", "2.0", "application/json");
        MessageContractDescriptor expected = new("ProductLifecycleEvent", "1.2", "application/json", Compatibility: CompatibilityMode.Full);

        bool compatible = MessageContractCompatibility.IsCompatible(actual, expected);

        compatible.Should().BeFalse();
    }

    [Fact]
    public void IsCompatible_ShouldRespectSupportedVersionsFilter()
    {
        MessageContractDescriptor actual = new("ProductLifecycleEvent", "1.0", "application/json");
        MessageContractDescriptor expected = new("ProductLifecycleEvent", "1.2", "application/json", Compatibility: CompatibilityMode.Full);

        bool compatible = MessageContractCompatibility.IsCompatible(
            actual,
            expected,
            supportedVersions: ["1.2", "1.3"]);

        compatible.Should().BeFalse();
    }

    [Fact]
    public void IsCompatible_ShouldRespectSupportedMessageTypeFilter()
    {
        MessageContractDescriptor actual = new("ProductCreatedIntegrationEvent", "1.0", "application/json");
        MessageContractDescriptor expected = new("ProductLifecycleEvent", "1.0", "application/json", Compatibility: CompatibilityMode.Full);

        bool compatible = MessageContractCompatibility.IsCompatible(
            actual,
            expected,
            supportedMessageType: "ProductUpdatedIntegrationEvent");

        compatible.Should().BeFalse();
    }

    [Fact]
    public void IsCompatible_ShouldSupportForwardMode()
    {
        MessageContractDescriptor actual = new("ProductLifecycleEvent", "1.4", "application/json");
        MessageContractDescriptor expected = new("ProductLifecycleEvent", "1.2", "application/json", Compatibility: CompatibilityMode.Forward);

        bool compatible = MessageContractCompatibility.IsCompatible(actual, expected);

        compatible.Should().BeTrue();
    }

    [Fact]
    public void IsCompatible_ShouldRequireExactVersion_ForNoneMode()
    {
        MessageContractDescriptor actual = new("ProductLifecycleEvent", "1.1", "application/json");
        MessageContractDescriptor expected = new("ProductLifecycleEvent", "1.2", "application/json", Compatibility: CompatibilityMode.None);

        bool compatible = MessageContractCompatibility.IsCompatible(actual, expected);

        compatible.Should().BeFalse();
    }

    [Fact]
    public void IsCompatible_ShouldFallbackToStringComparison_WhenVersionParsingFails()
    {
        MessageContractDescriptor actual = new("ProductLifecycleEvent", "v-next", "application/json");
        MessageContractDescriptor expected = new("ProductLifecycleEvent", "v-next", "application/json", Compatibility: CompatibilityMode.Full);

        bool compatible = MessageContractCompatibility.IsCompatible(actual, expected);

        compatible.Should().BeTrue();
    }
}
