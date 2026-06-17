using Auth.Api.Infrastructure.Configuration;
using Auth.Api.Infrastructure.Security;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Auth.Api.Features.Auth.Internal;

internal sealed class ValidateWorkloadEndpoint : IEndpoint
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapAuthV1()
            .MapGet("/internal/workload/validate", HandleAsync)
            .WithName(AuthRouteNames.ValidateWorkload)
            .RequireAuthorization(AuthPolicyNames.WorkloadOnly);
    }

    private static Results<Ok<WorkloadValidationResponse>, ProblemHttpResult> HandleAsync(
        HttpContext context,
        IOptions<WorkloadAuthOptions> workloadOptions,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        ClaimsPrincipal user = context.User;

        string clientId = user.FindFirstValue("azp")
            ?? user.FindFirstValue("client_id")
            ?? string.Empty;

        if (string.IsNullOrWhiteSpace(clientId))
        {
            return TypedResults.Problem(
                detail: "Missing azp/client_id claim.",
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid workload token");
        }

        WorkloadClientOptions? configuredClient = workloadOptions.Value.Clients
            .SingleOrDefault(x => string.Equals(x.ClientId, clientId, StringComparison.Ordinal));

        if (configuredClient is null)
        {
            return TypedResults.Problem(
                detail: "Client is not allowed for workload validation.",
                statusCode: StatusCodes.Status403Forbidden,
                title: "Access denied");
        }

        string[] tokenScopes = ExtractScopes(user);

        bool hasAllowedScope = tokenScopes.Any(scope => configuredClient.AllowedScopes.Contains(scope, StringComparer.Ordinal));
        if (!hasAllowedScope)
        {
            return TypedResults.Problem(
                detail: "Token does not contain an allowed scope.",
                statusCode: StatusCodes.Status403Forbidden,
                title: "Access denied");
        }

        string[] audiences = user.FindAll("aud")
            .Select(claim => claim.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        DateTimeOffset? issuedAt = FromUnixTimeClaim(user, "iat");
        DateTimeOffset? expiresAt = FromUnixTimeClaim(user, "exp");

        WorkloadValidationResponse response = new(
            clientId,
            tokenScopes,
            audiences,
            issuedAt,
            expiresAt);

        return TypedResults.Ok(response);
    }

    private static string[] ExtractScopes(ClaimsPrincipal principal)
    {
        IEnumerable<string> values = principal.FindAll("scope")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        return values.Distinct(StringComparer.Ordinal).ToArray();
    }

    private static DateTimeOffset? FromUnixTimeClaim(ClaimsPrincipal principal, string claimType)
    {
        string? raw = principal.FindFirstValue(claimType);
        if (!long.TryParse(raw, out long unixSeconds))
        {
            return null;
        }

        return DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
    }
}
