using SharedValidationException = Common.SharedKernel.Exceptions.ValidationException;

namespace Products.Api.Infrastructure;

internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : class, IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));
        var failures = validationResults.SelectMany(result => result.Errors).Where(failure => failure is not null).ToArray();

        if (failures.Length != 0)
        {
            string message = string.Join(
                "; ",
                failures.Select(failure => string.IsNullOrWhiteSpace(failure.PropertyName)
                    ? failure.ErrorMessage
                    : $"{failure.PropertyName}: {failure.ErrorMessage}"));

            throw new SharedValidationException(message);
        }

        return await next();
    }
}