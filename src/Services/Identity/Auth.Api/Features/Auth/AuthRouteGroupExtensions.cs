namespace Auth.Api.Features.Auth;

internal static class AuthRouteGroupExtensions
{
    public static RouteGroupBuilder MapAuthV1(this IEndpointRouteBuilder app)
    {
        return app.MapGroup("/api/v1/auth")
            .WithTags("Auth");
    }
}
