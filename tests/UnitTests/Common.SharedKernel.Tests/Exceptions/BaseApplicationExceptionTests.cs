using SharedExceptions = Common.SharedKernel.Exceptions;

namespace Common.SharedKernel.Tests.Exceptions;

public sealed class BaseApplicationExceptionTests
{
    [Fact]
    public void BaseException_ShouldPreserveMessage()
    {
        var exception = new TestException("boom");

        Assert.Equal("boom", exception.Message);
    }

    [Fact]
    public void NotFoundException_ShouldExposeDetails()
    {
        var exception = new SharedExceptions.NotFoundException("Order", "123");

        Assert.Equal("Order", exception.ResourceName);
        Assert.Equal("123", exception.ResourceKey);
        Assert.Contains("Order '123' was not found.", exception.Message);
    }

    [Fact]
    public void ValidationException_ShouldPreserveMessage()
    {
        var exception = new SharedExceptions.ValidationException("Invalid data");

        Assert.Equal("Invalid data", exception.Message);
    }

    [Fact]
    public void ConflictException_ShouldPreserveMessage()
    {
        var exception = new SharedExceptions.ConflictException("Conflict");

        Assert.Equal("Conflict", exception.Message);
    }

    private sealed class TestException(string message) : SharedExceptions.BaseApplicationException(message);
}