using System.Security.Cryptography;
using System.Text;

namespace Auth.Api.Infrastructure.Security;

internal static class RefreshTokenCrypto
{
    public static string GenerateToken()
    {
        byte[] bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }

    public static string Hash(string token)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hash);
    }
}
