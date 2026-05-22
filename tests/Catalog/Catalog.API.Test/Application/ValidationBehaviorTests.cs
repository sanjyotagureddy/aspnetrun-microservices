using Catalog.API.Application.Behaviors;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using SharedKernelValidationException = SharedKernel.Exceptions.ValidationException;
using Xunit;

namespace Catalog.API.Test;

public class ValidationBehaviorTests
{
    public sealed record TestRequest(string Value) : IRequest<string>;

    [Fact]
    public async Task Handle_WhenNoValidatorsExist_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>(Array.Empty<IValidator<TestRequest>>());
        var nextCalled = false;

        var result = await behavior.Handle(new TestRequest("ok"), cancellationToken =>
        {
            nextCalled = true;
            return Task.FromResult("done");
        }, CancellationToken.None);

        nextCalled.Should().BeTrue();
        result.Should().Be("done");
    }

    [Fact]
    public async Task Handle_WhenValidatorsPass_CallsNext()
    {
        var validator = new Mock<IValidator<TestRequest>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var behavior = new ValidationBehavior<TestRequest, string>([validator.Object]);

        var result = await behavior.Handle(new TestRequest("ok"), _ => Task.FromResult("done"), CancellationToken.None);

        result.Should().Be("done");
        validator.Verify(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenValidatorsFail_ThrowsValidationException()
    {
        var validator = new Mock<IValidator<TestRequest>>();
        validator.Setup(v => v.ValidateAsync(It.IsAny<IValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult([new ValidationFailure(nameof(TestRequest.Value), "Value is invalid")]));

        var behavior = new ValidationBehavior<TestRequest, string>([validator.Object]);

        var exception = await FluentActions.Awaiting(() => behavior.Handle(new TestRequest("bad"), _ => Task.FromResult("done"), CancellationToken.None))
            .Should().ThrowAsync<SharedKernelValidationException>();

        exception.Which.Message.Should().Contain("Validation failed.");
    }
}