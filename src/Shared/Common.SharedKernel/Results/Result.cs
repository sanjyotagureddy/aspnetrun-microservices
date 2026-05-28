using Common.SharedKernel.Helpers;

namespace Common.SharedKernel.Results;

public class Result
{
    protected Result(bool isSuccess, string error)
    {
        if (isSuccess && error.Length != 0)
        {
            throw new Common.SharedKernel.Exceptions.ValidationException(nameof(error), "A successful result cannot contain an error.");
        }

        if (!isSuccess && error.Length == 0)
        {
            throw new Common.SharedKernel.Exceptions.ValidationException(nameof(error), "A failed result must contain an error.");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public string Error { get; }

    public static Result Success() => new(true, string.Empty);

    public static Result Failure(string error) => new(false, Guard.Against.NullOrWhiteSpace(error, nameof(error)));
}

public sealed class Result<T> : Result
{
    private Result(bool isSuccess, T? value, string error)
        : base(isSuccess, error)
    {
        Value = value;
    }

    public T? Value { get; }

    public static Result<T> Success(T value) => new(true, value, string.Empty);

    public new static Result<T> Failure(string error) => new(false, default, Guard.Against.NullOrWhiteSpace(error, nameof(error)));
}