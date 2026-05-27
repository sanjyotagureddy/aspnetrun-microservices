using ValidationException = Common.SharedKernel.Exceptions.ValidationException;

namespace Common.SharedKernel.Validation;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next(cancellationToken);
        }

        var context = new ValidationContext<TRequest>(request);
        FluentValidation.Results.ValidationResult[] validationResults = await Task.WhenAll(validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));
        ValidationFailure[] failures = validationResults.SelectMany(result => result.Errors).Where(failure => failure is not null).ToArray();

        if (failures.Length == 0) return await next(cancellationToken);
        {
            var message = string.Join(
                "; ",
                failures.Select(failure => string.IsNullOrWhiteSpace(failure.PropertyName)
                    ? failure.ErrorMessage
                    : $"{failure.PropertyName}: {failure.ErrorMessage}"));

            throw new ValidationException(message);
        }

    }
}
