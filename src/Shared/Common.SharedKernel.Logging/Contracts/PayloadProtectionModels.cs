namespace Common.SharedKernel.Logging;

public sealed record PayloadRule(
    string Pattern,
    PayloadRuleMatchType MatchType,
    PayloadRuleAction Action,
    string? CustomStrategyName = null);

public sealed record PayloadProtectionRequest(
    object? Payload,
    string Source,
    string? ContentType = null,
    IReadOnlyCollection<PayloadRule>? Rules = null,
    IReadOnlyCollection<string>? GlobalSensitiveFields = null,
    string? CorrelationId = null,
    string? TraceId = null);

public sealed record PayloadProtectionFailure(
    string Code,
    string Message,
    PayloadProtectionFailureBehavior Behavior);

public sealed record PayloadProtectionResult(
    bool Success,
    object? ProtectedPayload,
    int MaskedFieldCount,
    int RedactedFieldCount,
    PayloadProtectionFailure? Failure = null);

public sealed record PayloadStoreWriteRequest(
    object ProtectedPayload,
    string ContentType,
    string? CorrelationId = null,
    string? TraceId = null);

public sealed record PayloadStoreWriteResult(
    string PayloadRef,
    string PayloadHash,
    long PayloadSizeBytes,
    string PayloadEncoding,
    bool Compressed,
    bool Encrypted);
