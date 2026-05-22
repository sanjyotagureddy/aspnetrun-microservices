namespace SharedKernel.Context;

public static class RequestContextScope
{
    private static readonly AsyncLocal<Stack<RequestContext>> Stack = new();
    private static readonly RequestContext Empty = new("unknown-service");

    private static Stack<RequestContext> ContextStack => Stack.Value ??= [];

    public static RequestContext Current => ContextStack.Count > 0
        ? ContextStack.Peek()
        : Empty;

    public static IDisposable BeginScope(RequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ContextStack.Push(context);
        return new DisposableScope(ContextStack);
    }

    private sealed class DisposableScope(Stack<RequestContext> contextStack) : IDisposable
    {
        private bool disposed;

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            if (contextStack.Count > 0)
            {
                contextStack.Pop();
            }

            disposed = true;
        }
    }
}
