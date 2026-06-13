namespace Auth.Api.Features.Auth.Health;

internal sealed class GetAuthHealthEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapAuthV1();

        group.MapGet("/health", HandleAsync)
            .WithName(AuthRouteNames.GetHealth);
    }

    private static async Task<IResult> HandleAsync(IMediator mediator, CancellationToken cancellationToken)
    {
        Result<AuthHealthResponse> result = await mediator.Send(new GetAuthHealthQuery(), cancellationToken);
        return TypedResults.Ok(result.Value);
    }
}
