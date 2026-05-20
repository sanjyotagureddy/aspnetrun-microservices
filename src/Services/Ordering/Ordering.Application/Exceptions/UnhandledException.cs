namespace Ordering.Application.Exceptions;

public class UnhandledException(string name, object key)
  : ApplicationException($"An Unknown error occurred for \"{name}\" ({key})");