using Inventory.Api.Observability;

namespace Inventory.Api.Tests.Observability;

public sealed class AppCallContextTests
{
    [Fact]
    public void Ctor_ShouldSetTenantId_WhenProvided()
    {
        AppCallContext context = new("corr", tenantId: "tenant-1");

        Assert.Equal("tenant-1", context.TenantId);
    }

    [Fact]
    public void Ctor_ShouldSetTenantIdNull_WhenWhitespace()
    {
        AppCallContext context = new("corr", tenantId: "   ");

        Assert.Null(context.TenantId);
    }
}
