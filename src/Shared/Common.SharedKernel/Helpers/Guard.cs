namespace Common.SharedKernel.Helpers;

using System.Runtime.CompilerServices;

public static class Guard
{
    public static class Against
    {
        public static T Null<T>(T? input, [CallerArgumentExpression("input")] string? parameterExpression = null)
            where T : class
        {
            if (input is not null)
                return input;

            var paramName = parameterExpression ?? typeof(T).Name;
            var message = $"Required value '{paramName}' was null. Expected a non-null {typeof(T).FullName}.";
            throw new Common.SharedKernel.Exceptions.ValidationException(paramName, message);
        }

        public static string NullOrWhiteSpace(string? input, [CallerArgumentExpression("input")] string? parameterExpression = null)
        {
            if (!string.IsNullOrWhiteSpace(input))
                return input!;

            var paramName = parameterExpression ?? "input";
            var message = $"Value '{paramName}' cannot be null or whitespace.";
            throw new Common.SharedKernel.Exceptions.ValidationException(paramName, message);
        }

        public static int Negative(int input, [CallerArgumentExpression("input")] string? parameterExpression = null)
        {
            if (input >= 0)
            {
                return input;
            }

            var paramName = parameterExpression ?? "input";
            var message = $"Value '{paramName}' must be greater than or equal to 0. Actual: {input}.";
            throw new Common.SharedKernel.Exceptions.ValidationException(paramName, message);
        }
    }
}
