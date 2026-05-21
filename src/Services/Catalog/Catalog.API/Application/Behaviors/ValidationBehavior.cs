using FluentValidation;
using MediatR;
using SharedKernel;
using SharedKernel.Errors;
using SharedKernelValidationException = SharedKernel.Exceptions.ValidationException;

namespace Catalog.API.Application.Behaviors;

internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators = validators;

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(_validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));
        var failures = validationResults.SelectMany(result => result.Errors).Where(error => error is not null).ToArray();

        if (failures.Length > 0)
        {
            throw new SharedKernelValidationException(
                Constants.ServiceCodes.Catalog,
                "Validation failed.",
                failures.Select(error => new Info(error.PropertyName, error.ErrorMessage)));
        }

        return await next(cancellationToken);
    }
}
