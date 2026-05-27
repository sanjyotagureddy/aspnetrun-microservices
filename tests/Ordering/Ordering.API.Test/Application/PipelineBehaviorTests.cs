using FluentValidation;

using MediatR;

using Microsoft.Extensions.Logging.Abstractions;
using Ordering.Application.Behaviors;
using Ordering.Application.Exceptions;
using AppValidationException = Ordering.Application.Exceptions.ValidationException;

using Xunit;

namespace Ordering.API.Test.Application;

public sealed class PipelineBehaviorTests
{
    [Fact]
    public async Task ValidationBehavior_WithNoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>(Array.Empty<IValidator<TestRequest>>());
        Task<string> Next(CancellationToken _) => Task.FromResult("done");

        var result = await behavior.Handle(
            new TestRequest("ok"),
            Next,
            CancellationToken.None);

        Assert.Equal("done", result);
    }

    [Fact]
    public async Task ValidationBehavior_WithValidationFailure_ThrowsValidationException()
    {
        var validator = new InlineValidator<TestRequest>();
        validator.RuleFor(value => value.Name).NotEmpty();

        var behavior = new ValidationBehavior<TestRequest, string>(new[] { validator });
        RequestHandlerDelegate<string> next = _ => Task.FromResult("never");

        await Assert.ThrowsAsync<AppValidationException>(() =>
            behavior.Handle(new TestRequest(string.Empty), next, CancellationToken.None));
    }

    [Fact]
    public async Task UnhandledExceptionBehavior_ReturnsNextResult_WhenNoException()
    {
        var behavior = new UnhandledExceptionBehavior<TestRequest, string>(NullLogger<TestRequest>.Instance);
        RequestHandlerDelegate<string> next = _ => Task.FromResult("done");

        string result = await behavior.Handle(new TestRequest("ok"), next, CancellationToken.None);

        Assert.Equal("done", result);
    }

    [Fact]
    public async Task UnhandledExceptionBehavior_WrapsUnhandledExceptions()
    {
        var behavior = new UnhandledExceptionBehavior<TestRequest, string>(NullLogger<TestRequest>.Instance);
        RequestHandlerDelegate<string> next = _ => Task.FromException<string>(new InvalidOperationException("unexpected"));

        UnhandledException exception = await Assert.ThrowsAsync<UnhandledException>(() =>
            behavior.Handle(new TestRequest("boom"), next, CancellationToken.None));

        Assert.Contains(nameof(TestRequest), exception.Message);
    }

    private sealed record TestRequest(string Name) : IRequest<string>;
}
