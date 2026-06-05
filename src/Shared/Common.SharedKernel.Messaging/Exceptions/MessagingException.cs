namespace Common.SharedKernel.Messaging;

public class MessagingException(string message, Exception? innerException = null)
    : Exception(message, innerException);
