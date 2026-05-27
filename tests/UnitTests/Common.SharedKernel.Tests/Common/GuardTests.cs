using Common.SharedKernel.Helpers;

namespace Common.SharedKernel.Tests.Common;

public sealed class GuardTests
{
    [Fact]
    public void NullOrWhiteSpace_ShouldReturnOriginalValue()
    {
        var value = Guard.Against.NullOrWhiteSpace("abc", "value");

        Assert.Equal("abc", value);
    }

    [Fact]
    public void Negative_ShouldThrowWhenValueIsNegative()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Guard.Against.Negative(-1, "value"));
    }
}