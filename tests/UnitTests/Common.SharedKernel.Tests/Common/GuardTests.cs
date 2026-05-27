using Common.SharedKernel.Exceptions;
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
        int value = -1;
        var ex = Assert.Throws<ValidationException>(() => Guard.Against.Negative(value));
        Assert.Equal("value", ex.ParamName);
        Assert.Contains("Actual: -1", ex.Message);
    }

    [Fact]
    public void NullOrWhiteSpace_ShouldThrowIncludeCallerExpression()
    {
        string? s = null;
        var ex = Assert.Throws<ValidationException>(() => Guard.Against.NullOrWhiteSpace(s));
        Assert.Equal("s", ex.ParamName);
        Assert.Contains("cannot be null or whitespace", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}