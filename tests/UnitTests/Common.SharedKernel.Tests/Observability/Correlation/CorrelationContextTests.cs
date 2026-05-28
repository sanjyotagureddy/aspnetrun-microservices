using Common.SharedKernel.Observability.Context;

namespace Common.SharedKernel.Tests.Observability.Correlation;

public sealed class AppCallContextBaseTests
{
    [Fact]
    public void Create_ShouldNormalizeEmptyOptionalValues()
    {
        var context = new TestApiCallContext("corr-123", "", "   ", null);

        Assert.Equal("corr-123", context.CorrelationId);
        Assert.Null(context.ParentCorrelationId);
        Assert.Null(context.TraceId);
        Assert.Null(context.SpanId);
    }

    [Fact]
    public void Create_ShouldPreserveOptionalValues()
    {
        var context = new TestApiCallContext("corr-123", "parent-456", "trace-789", "span-abc");

        Assert.Equal("parent-456", context.ParentCorrelationId);
        Assert.Equal("trace-789", context.TraceId);
        Assert.Equal("span-abc", context.SpanId);
    }

    private sealed class TestApiCallContext(
        string correlationId,
        string? parentCorrelationId = null,
        string? traceId = null,
        string? spanId = null,
        IDictionary<string, string>? headers = null,
        IDictionary<string, object?>? items = null) : AppCallContextBase(correlationId, parentCorrelationId, traceId, spanId, headers, items)
    {
    }
}
