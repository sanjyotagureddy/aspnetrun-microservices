namespace LogStore.Api.Features.PayloadProtection;

internal sealed class ProtectPayloadEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapLogStorageV1();

        group.MapPost("/", CreateAsync)
            .WithName("CreateLog");

        group.MapGet("/{id}", GetByIdAsync)
            .WithName("GetLog");
    }

    private static async Task<IResult> CreateAsync(
        CreateLogRequest request,
        ILogStorageService logStorageService,
        CancellationToken cancellationToken)
    {
        CreateLogResponse response = await logStorageService.CreateAsync(request, cancellationToken);
        return TypedResults.Ok(response);
    }

    private static async Task<IResult> GetByIdAsync(
        string id,
        ILogStorageService logStorageService,
        CancellationToken cancellationToken)
    {
        GetLogResponse response = await logStorageService.GetAsync(id, cancellationToken);
        return TypedResults.Ok(response);
    }
}
