namespace Common.SharedKernel.Logging;

public sealed record MaskingOptions
{
    public Dictionary<string, string> FieldMaskers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
