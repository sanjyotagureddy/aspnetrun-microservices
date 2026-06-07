namespace Common.SharedKernel.Logging;

public interface IMask
{
    string Name { get; }

    bool TryMask(string key, object? value, string maskValue, out object? maskedValue);
}
