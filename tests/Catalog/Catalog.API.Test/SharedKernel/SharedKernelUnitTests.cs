using FluentAssertions;
using SharedKernel;
using SharedKernel.Errors;
using SharedKernel.Exceptions;
using Xunit;

namespace Catalog.API.Test.SharedKernel;

public class SharedKernelUnitTests
{
    [Fact]
    public void BaseException_Constructs_Error_With_Info()
    {
        var info = new Info("X1", "detail");
        var ex = new NotFoundException(Constants.ServiceCodes.Catalog, "custom message", new[] { info });

        var err = ex.Error;

        err.Should().NotBeNull();
        err.Code.Should().Contain("404-");
        err.Description.Should().Be("custom message");
        err.Info.Should().NotBeEmpty();
        err.Info[0].Code.Should().Be("X1");
    }

    [Theory]
    [MemberData(nameof(ServerSideExceptionCases))]
    public void ServerSide_Factories_Create_Exception_With_AutoDetected_ServiceCode(Type expectedType, Func<BaseException> factory, string expectedCode)
    {
        ValidateException(factory(), expectedType, expectedCode);
    }

    [Theory]
    [MemberData(nameof(ClientSideExceptionCases))]
    public void ClientSide_Factories_Create_Exception_With_AutoDetected_ServiceCode(Type expectedType, Func<BaseException> factory, string expectedCode)
    {
        ValidateException(factory(), expectedType, expectedCode);
    }

    [Theory]
    [MemberData(nameof(CommonExceptionCases))]
    public void Common_Factories_Create_Exception_With_AutoDetected_ServiceCode(Type expectedType, Func<BaseException> factory, string expectedCode)
    {
        ValidateException(factory(), expectedType, expectedCode);
    }

    public static IEnumerable<object[]> ServerSideExceptionCases()
    {
        yield return new object[] { typeof(ConfigurationMissingException), new Func<BaseException>(() => Errors.ServerSide.ConfigurationMissing("custom message", new[] { new Info("X1", "detail") })), "500-09-01" };
        yield return new object[] { typeof(ValidationException), new Func<BaseException>(() => Errors.ServerSide.Validation("custom message", new[] { new Info("X1", "detail") })), "400-01-01" };
        yield return new object[] { typeof(NotFoundException), new Func<BaseException>(() => Errors.ServerSide.NotFound("custom message", new[] { new Info("X1", "detail") })), "404-02-01" };
        yield return new object[] { typeof(ConflictException), new Func<BaseException>(() => Errors.ServerSide.Conflict("custom message", new[] { new Info("X1", "detail") })), "409-03-01" };
        yield return new object[] { typeof(UnauthorizedException), new Func<BaseException>(() => Errors.ServerSide.Unauthorized("custom message", new[] { new Info("X1", "detail") })), "401-04-01" };
        yield return new object[] { typeof(ForbiddenException), new Func<BaseException>(() => Errors.ServerSide.Forbidden("custom message", new[] { new Info("X1", "detail") })), "403-05-01" };
        yield return new object[] { typeof(DependencyFailureException), new Func<BaseException>(() => Errors.ServerSide.DependencyFailure("custom message", new[] { new Info("X1", "detail") })), "502-07-01" };
        yield return new object[] { typeof(IdempotencyConflictException), new Func<BaseException>(() => Errors.ServerSide.IdempotencyConflict("custom message", new[] { new Info("X1", "detail") })), "409-08-01" };
        yield return new object[] { typeof(BaseException), new Func<BaseException>(() => Errors.ServerSide.Unknown("custom message", new[] { new Info("X1", "detail") })), "500-00-01" };
    }

    public static IEnumerable<object[]> ClientSideExceptionCases()
    {
        yield return new object[] { typeof(ValidationException), new Func<BaseException>(() => Errors.ClientSide.Validation("custom message", new[] { new Info("X1", "detail") })), "400-01-01" };
        yield return new object[] { typeof(NotFoundException), new Func<BaseException>(() => Errors.ClientSide.NotFound("custom message", new[] { new Info("X1", "detail") })), "404-02-01" };
        yield return new object[] { typeof(ConflictException), new Func<BaseException>(() => Errors.ClientSide.Conflict("custom message", new[] { new Info("X1", "detail") })), "409-03-01" };
        yield return new object[] { typeof(UnauthorizedException), new Func<BaseException>(() => Errors.ClientSide.Unauthorized("custom message", new[] { new Info("X1", "detail") })), "401-04-01" };
        yield return new object[] { typeof(ForbiddenException), new Func<BaseException>(() => Errors.ClientSide.Forbidden("custom message", new[] { new Info("X1", "detail") })), "403-05-01" };
        yield return new object[] { typeof(IdempotencyConflictException), new Func<BaseException>(() => Errors.ClientSide.IdempotencyConflict("custom message", new[] { new Info("X1", "detail") })), "409-08-01" };
    }

    public static IEnumerable<object[]> CommonExceptionCases()
    {
        yield return new object[] { typeof(ConfigurationMissingException), new Func<BaseException>(() => Errors.Common.ConfigurationMissing("custom message", new[] { new Info("X1", "detail") })), "500-09-01" };
        yield return new object[] { typeof(ValidationException), new Func<BaseException>(() => Errors.Common.Validation("custom message", new[] { new Info("X1", "detail") })), "400-01-01" };
        yield return new object[] { typeof(NotFoundException), new Func<BaseException>(() => Errors.Common.NotFound("custom message", new[] { new Info("X1", "detail") })), "404-02-01" };
        yield return new object[] { typeof(ConflictException), new Func<BaseException>(() => Errors.Common.Conflict("custom message", new[] { new Info("X1", "detail") })), "409-03-01" };
        yield return new object[] { typeof(UnauthorizedException), new Func<BaseException>(() => Errors.Common.Unauthorized("custom message", new[] { new Info("X1", "detail") })), "401-04-01" };
        yield return new object[] { typeof(ForbiddenException), new Func<BaseException>(() => Errors.Common.Forbidden("custom message", new[] { new Info("X1", "detail") })), "403-05-01" };
        yield return new object[] { typeof(DependencyFailureException), new Func<BaseException>(() => Errors.Common.DependencyFailure("custom message", new[] { new Info("X1", "detail") })), "502-07-01" };
        yield return new object[] { typeof(IdempotencyConflictException), new Func<BaseException>(() => Errors.Common.IdempotencyConflict("custom message", new[] { new Info("X1", "detail") })), "409-08-01" };
        yield return new object[] { typeof(BaseException), new Func<BaseException>(() => Errors.Common.Unknown("custom message", new[] { new Info("X1", "detail") })), "500-00-01" };
    }

    private static void ValidateException(BaseException exception, Type expectedType, string expectedCode)
    {
        exception.Should().BeOfType(expectedType);
        exception.HttpStatus.Should().Be(int.Parse(expectedCode.Split('-')[0]));
        exception.ErrorCode.Should().Be(expectedCode);
        exception.Error.Code.Should().Be(expectedCode);
        exception.Error.Description.Should().Be("custom message");
        exception.Error.Info.Should().HaveCount(1);
        exception.Error.Info[0].Code.Should().Be("X1");
        exception.Error.Info[0].Description.Should().Be("detail");
    }
}