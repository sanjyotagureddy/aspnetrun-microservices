namespace Auth.Api.Features.Auth.Health;

internal sealed class GetAuthHealthEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapAuthV1();

        group.MapGet("/health", HandleAsync)
            .WithName(AuthRouteNames.GetHealth);
    }

    private static async Task<Microsoft.AspNetCore.Http.HttpResults.Results<Microsoft.AspNetCore.Http.HttpResults.Ok<AuthHealthResponse>, Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>> HandleAsync(IMediator mediator, CancellationToken cancellationToken)
    {
        Result<AuthHealthResponse> result = await mediator.Send(new GetAuthHealthQuery(), cancellationToken);

        return result.IsSuccess
            ? TypedResults.Ok(result.Value)
            : TypedResults.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Auth service dependencies unavailable");
    }
}
