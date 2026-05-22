using FluentAssertions;
using SharedKernel;
using SharedKernel.Errors;
using SharedKernel.Exceptions;
using Xunit;

namespace Catalog.API.Test;

public class SharedKernelUnitTests
{
    [Fact]
    public void BaseException_Constructs_Error_With_Info()
    {
        var info = new Info("X1", "detail");
        var ex = new NotFoundException(Constants.ServiceCodes.Catalog, "custom message", new[] { info });

        var err = ex.Error;

        err.Should().NotBeNull();
        err.Code.Should().Contain("404_");
        err.Description.Should().Be("custom message");
        err.Info.Should().NotBeEmpty();
        err.Info[0].Code.Should().Be("X1");
    }
}