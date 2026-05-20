using FluentValidation.Results;

namespace Ordering.Application.Exceptions;

public class ValidationException() : ApplicationException("One or more validation failures have occurred.")
{
  public ValidationException(IEnumerable<ValidationFailure> failures)
    : this()
  {
    Errors = failures
      .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
      .ToDictionary(fg => fg.Key, fg => fg.ToArray());
  }

  public Dictionary<string, string[]> Errors { get; } = new();
}