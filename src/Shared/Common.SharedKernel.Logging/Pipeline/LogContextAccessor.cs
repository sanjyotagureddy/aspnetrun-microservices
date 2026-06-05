namespace Common.SharedKernel.Logging;

internal sealed class LogContextAccessor : ILogContextAccessor
{
    private static readonly AsyncLocal<ScopeState?> CurrentState = new();

    public LogContext? Current => CurrentState.Value?.Context;

    public IDisposable BeginScope(LogContext context)
    {
        Guard.Against.Null(context);

        ScopeState? previous = CurrentState.Value;
        CurrentState.Value = new ScopeState(context, previous);
        return new Scope(previous);
    }

    private sealed record ScopeState(LogContext Context, ScopeState? Parent);

    private sealed class Scope(ScopeState? previous) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            CurrentState.Value = previous;
        }
    }
}
