using Common.SharedKernel.Abstractions.ValueObjects;

namespace Common.SharedKernel.Tests.Abstractions.ValueObjects;

public sealed class ValueObjectTests
{
    [Fact]
    public void ValueObjects_WithSameComponents_ShouldBeEqual()
    {
        var first = new Money(10.50m, "USD");
        var second = new Money(10.50m, "USD");

        Assert.Equal(first, second);
        Assert.True(first == second);
    }

    [Fact]
    public void ValueObjects_WithDifferentComponents_ShouldNotBeEqual()
    {
        var first = new Money(10.50m, "USD");
        var second = new Money(11.00m, "USD");

        Assert.NotEqual(first, second);
        Assert.True(first != second);
    }

    private sealed class Money(decimal amount, string currency) : ValueObject
    {
        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return amount;
            yield return currency;
        }
    }
}