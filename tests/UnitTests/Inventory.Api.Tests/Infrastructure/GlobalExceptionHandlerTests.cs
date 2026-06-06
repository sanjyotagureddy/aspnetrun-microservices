using System.Text.Json;
using Common.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Http;
using Moq;
using Inventory.Api.Infrastructure;

namespace Inventory.Api.Tests.Infrastructure;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ShouldReturnBadRequest_ForValidationException()
    {
        Mock<Common.SharedKernel.Logging.ILogger<GlobalExceptionHandler>> logger = new();
        GlobalExceptionHandler handler = new(logger.Object);
        DefaultHttpContext httpContext = new();
        httpContext.Response.Body = new MemoryStream();

        bool handled = await handler.TryHandleAsync(httpContext, new ValidationException("field", "invalid"), TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", httpContext.Response.ContentType);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnNotFound_ForNotFoundException()
    {
        Mock<Common.SharedKernel.Logging.ILogger<GlobalExceptionHandler>> logger = new();
        GlobalExceptionHandler handler = new(logger.Object);
        DefaultHttpContext httpContext = new();
        httpContext.Response.Body = new MemoryStream();

        bool handled = await handler.TryHandleAsync(httpContext, new NotFoundException("Inventory", "id"), TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnConflict_ForConflictException()
    {
        Mock<Common.SharedKernel.Logging.ILogger<GlobalExceptionHandler>> logger = new();
        GlobalExceptionHandler handler = new(logger.Object);
        DefaultHttpContext httpContext = new();
        httpContext.Response.Body = new MemoryStream();

        bool handled = await handler.TryHandleAsync(httpContext, new ConflictException("conflict"), TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnInternalServerError_ForUnknownException()
    {
        Mock<Common.SharedKernel.Logging.ILogger<GlobalExceptionHandler>> logger = new();
        GlobalExceptionHandler handler = new(logger.Object);
        DefaultHttpContext httpContext = new();
        httpContext.TraceIdentifier = "trace-123";
        httpContext.Response.Body = new MemoryStream();

        bool handled = await handler.TryHandleAsync(httpContext, new Exception("boom"), TestContext.Current.CancellationToken);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);

        httpContext.Response.Body.Position = 0;
        using StreamReader reader = new(httpContext.Response.Body);
        string json = await reader.ReadToEndAsync();
        using JsonDocument doc = JsonDocument.Parse(json);
        Assert.Equal("trace-123", doc.RootElement.GetProperty("traceId").GetString());
    }
}
