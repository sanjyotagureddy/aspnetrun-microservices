namespace Common.SharedKernel.Logging.UnitTests;

public sealed class LogContextAccessorTests
{
    [Fact]
    public void BeginScope_ShouldExposeCurrentContextUntilDisposed()
    {
        LogContextAccessor accessor = new();
        LogContext context = new()
        {
            CorrelationId = "corr-1",
            TenantId = "tenant-1"
        };

        using IDisposable scope = accessor.BeginScope(context);

        accessor.Current.Should().BeSameAs(context);

        scope.Dispose();

        accessor.Current.Should().BeNull();
    }

    [Fact]
    public void BeginScope_ShouldRestoreParentScope()
    {
        LogContextAccessor accessor = new();
        LogContext outer = new() { CorrelationId = "outer" };
        LogContext inner = new() { CorrelationId = "inner" };

        using IDisposable outerScope = accessor.BeginScope(outer);
        using IDisposable innerScope = accessor.BeginScope(inner);

        accessor.Current.Should().BeSameAs(inner);

        innerScope.Dispose();

        accessor.Current.Should().BeSameAs(outer);
        outerScope.Dispose();

        accessor.Current.Should().BeNull();
    }
}
