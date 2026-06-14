using Dapper;
using Auth.Api.Infrastructure.Security;

namespace Auth.Api.Features.Auth.Profile;

internal sealed class GetMyProfileEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app.MapAuthV1();

        group.MapGet("/me", HandleAsync)
            .WithName(AuthRouteNames.GetMyProfile)
            .RequireAuthorization(AuthPolicyNames.UserOnly);
    }

    private static async Task<Microsoft.AspNetCore.Http.HttpResults.Results<Microsoft.AspNetCore.Http.HttpResults.Ok<UserProfileResponse>, Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult>> HandleAsync(HttpContext httpContext, IMediator mediator, CancellationToken cancellationToken)
    {
        Result<UserProfileResponse> result = await mediator.Send(new GetMyProfileQuery(httpContext.User), cancellationToken);

        if (result.IsFailure)
        {
            return TypedResults.Problem(
                detail: result.Error,
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Authentication required");
        }

        return TypedResults.Ok(result.Value);
    }
}

internal sealed record GetMyProfileQuery(ClaimsPrincipal User) : IRequest<Result<UserProfileResponse>>;

internal sealed class GetMyProfileQueryHandler(NpgsqlDataSource dataSource)
    : IRequestHandler<GetMyProfileQuery, Result<UserProfileResponse>>
{
    public async Task<Result<UserProfileResponse>> Handle(GetMyProfileQuery request, CancellationToken cancellationToken)
    {
        string? subject = request.User.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Result<UserProfileResponse>.Failure("Missing subject claim.");
        }

        string? name = request.User.FindFirstValue("name");
        string? email = request.User.FindFirstValue("email");

        await using NpgsqlConnection connection = await dataSource.OpenConnectionAsync(cancellationToken);

        IReadOnlyCollection<MembershipRow> rows = (await connection.QueryAsync<MembershipRow>(new CommandDefinition(
            """
            select tenant_id as TenantId, role as Role
            from auth_user_tenant_memberships
            where subject = @Subject
            order by tenant_id, role
            """,
            new { Subject = subject },
            cancellationToken: cancellationToken))).AsList();

        IReadOnlyCollection<TenantMembershipResponse> memberships = rows
            .GroupBy(x => x.TenantId, StringComparer.Ordinal)
            .Select(group => new TenantMembershipResponse(group.Key, group.Select(x => x.Role).Distinct(StringComparer.Ordinal).ToArray()))
            .ToArray();

        UserProfileResponse response = new(subject, name, email, memberships);
        return Result<UserProfileResponse>.Success(response);
    }

    private sealed record MembershipRow(string TenantId, string Role);
}
