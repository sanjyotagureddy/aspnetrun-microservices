namespace Common.SharedKernel.Messaging;

public sealed class MessagingConfigurationException(string message)
    : MessagingException(message);
