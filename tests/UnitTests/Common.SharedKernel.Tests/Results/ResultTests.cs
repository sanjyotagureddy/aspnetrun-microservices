using Common.SharedKernel.Helpers;
using Common.SharedKernel.Results;

namespace Common.SharedKernel.Tests.Results;

public sealed class ResultTests
{
    [Fact]
    public void SuccessResult_ShouldBeSuccessful()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void FailureResult_ShouldContainError()
    {
        var result = Result.Failure("Something went wrong");

        Assert.True(result.IsFailure);
        Assert.Equal("Something went wrong", result.Error);
    }

    [Fact]
    public void GenericSuccessResult_ShouldCarryValue()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }
}