using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Gateway.Yarp.Security;

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "GatewayApiKey";
    public const string HeaderName = "X-Gateway-Key";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var providedValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        string? expectedApiKey = Context.RequestServices
            .GetRequiredService<IConfiguration>()
            ["GatewaySecurity:ApiKey"];

        if (string.IsNullOrWhiteSpace(expectedApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Gateway API key is not configured."));
        }

        string? providedApiKey = providedValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Gateway API key was not provided."));
        }

        if (!FixedTimeEquals(providedApiKey, expectedApiKey))
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid gateway API key."));
        }

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "gateway-client"),
            new Claim(ClaimTypes.Name, "gateway-client")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        byte[] leftBytes = Encoding.UTF8.GetBytes(left);
        byte[] rightBytes = Encoding.UTF8.GetBytes(right);

        return CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
