using Microsoft.AspNetCore.Builder;

namespace SharedKernel.Middleware;

public static class GlobalExceptionHandlerExtensions
{
    /// <summary>
    /// Registers the global exception handler middleware. Pass the padded service code (e.g. "01").
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app, string serviceCode)
    {
        return app.UseMiddleware<GlobalExceptionHandlerMiddleware>(serviceCode);
    }
}
