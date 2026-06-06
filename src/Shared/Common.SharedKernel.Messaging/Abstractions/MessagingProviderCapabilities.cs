namespace Common.SharedKernel.Messaging;

public sealed record MessagingProviderCapabilities(
    bool SupportsPartitioning,
    bool SupportsOrderingByKey,
    bool SupportsNativeDeadLetter,
    bool SupportsTransactions,
    bool SupportsDelayedDelivery);
