using Common.SharedKernel.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Common.SharedKernel.Web;

public static class AuthFailureExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseAuthFailureExceptionMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<AuthFailureExceptionMiddleware>();
    }
}

internal sealed class AuthFailureExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        if (context.Response.HasStarted)
        {
            return;
        }

        // Avoid overwriting responses that already include a payload.
        if (context.Response.ContentLength is > 0 || !string.IsNullOrWhiteSpace(context.Response.ContentType))
        {
            return;
        }

        if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
        {
            throw new AuthenticationFailedException();
        }

        if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
        {
            throw new AuthorizationDeniedException();
        }
    }
}
