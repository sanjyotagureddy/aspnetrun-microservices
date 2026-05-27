namespace Common.SharedKernel.Helpers;

public static class Guard
{
    public static class Against
    {
        public static T Null<T>(T? input, string parameterName)
            where T : class
        {
            return input ?? throw new ArgumentNullException(parameterName);
        }

        public static string NullOrWhiteSpace(string? input, string parameterName)
        {
            return !string.IsNullOrWhiteSpace(input)
                ? input
                : throw new ArgumentException("Value cannot be null or whitespace.", parameterName);
        }

        public static int Negative(int input, string parameterName)
        {
            return input >= 0
                ? input
                : throw new ArgumentOutOfRangeException(parameterName, input, "Value cannot be negative.");
        }
    }
}