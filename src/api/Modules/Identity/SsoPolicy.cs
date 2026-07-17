using System.Security.Cryptography;
using System.Text;

namespace OpenBusinessPlatform.Api.Modules.Identity;

public static class SsoPolicy
{
    public static string NormalizeReturnPath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "/";
        }

        var path = value.Trim();
        return path.StartsWith("/", StringComparison.Ordinal)
            && !path.StartsWith("//", StringComparison.Ordinal)
            && !path.Contains('\\')
            && Uri.TryCreate(path, UriKind.Relative, out _)
                ? path
                : "/";
    }

    public static string CreateRandomValue(int byteCount = 32)
    {
        return Base64UrlEncode(RandomNumberGenerator.GetBytes(byteCount));
    }

    public static string CreateCodeChallenge(string verifier)
    {
        return Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
