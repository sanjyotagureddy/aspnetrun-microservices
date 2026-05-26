using FluentValidation.TestHelper;

using Ordering.Application.Exceptions;
using Ordering.Application.Features.Orders.Commands.CheckoutOrder;
using Ordering.Application.Features.Orders.Commands.UpdateOrder;

using Xunit;

namespace Ordering.API.Test.Application;

public sealed class ValidatorsAndExceptionsTests
{
    [Fact]
    public void CheckoutOrderValidator_ReturnsErrors_ForInvalidCommand()
    {
        var validator = new CheckoutOrderCommandValidator();
        var command = new CheckoutOrderCommand
        {
            UserName = string.Empty,
            EmailAddress = "invalid-email",
            TotalPrice = 0
        };

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.UserName);
        result.ShouldHaveValidationErrorFor(c => c.EmailAddress);
        result.ShouldHaveValidationErrorFor(c => c.TotalPrice);
    }

    [Fact]
    public void CheckoutOrderValidator_HasNoErrors_ForValidCommand()
    {
        var validator = new CheckoutOrderCommandValidator();
        var command = new CheckoutOrderCommand
        {
            UserName = "swn",
            EmailAddress = "user@example.com",
            TotalPrice = 10
        };

        var result = validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.UserName);
        result.ShouldNotHaveValidationErrorFor(c => c.EmailAddress);
        result.ShouldNotHaveValidationErrorFor(c => c.TotalPrice);
    }

    [Fact]
    public void UpdateOrderValidator_ReturnsErrors_ForInvalidCommand()
    {
        var validator = new UpdateOrderCommandValidator();
        var command = new UpdateOrderCommand
        {
            UserName = string.Empty,
            EmailAddress = "invalid-email",
            TotalPrice = 0
        };

        var result = validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.UserName);
        result.ShouldHaveValidationErrorFor(c => c.EmailAddress);
        result.ShouldHaveValidationErrorFor(c => c.TotalPrice);
    }

    [Fact]
    public void UpdateOrderValidator_HasNoErrors_ForValidCommand()
    {
        var validator = new UpdateOrderCommandValidator();
        var command = new UpdateOrderCommand
        {
            UserName = "swn",
            EmailAddress = "user@example.com",
            TotalPrice = 100
        };

        var result = validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.UserName);
        result.ShouldNotHaveValidationErrorFor(c => c.EmailAddress);
        result.ShouldNotHaveValidationErrorFor(c => c.TotalPrice);
    }

    [Fact]
    public void NotFoundException_ContainsEntityNameAndKey()
    {
        var exception = new NotFoundException("Order", 5);

        Assert.Contains("Order", exception.Message);
        Assert.Contains("5", exception.Message);
    }

    [Fact]
    public void UnhandledException_ContainsRequestType()
    {
        var exception = new UnhandledException("TestRequest", new { Id = 1 });

        Assert.Contains("TestRequest", exception.Message);
    }

    [Fact]
    public void ValidationException_GroupsErrorsByProperty()
    {
        var failures = new[]
        {
            new FluentValidation.Results.ValidationFailure("EmailAddress", "Invalid email"),
            new FluentValidation.Results.ValidationFailure("EmailAddress", "Email required")
        };

        var exception = new ValidationException(failures);

        Assert.True(exception.Errors.ContainsKey("EmailAddress"));
        Assert.Equal(2, exception.Errors["EmailAddress"].Length);
    }
}
