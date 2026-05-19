using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using OpenBusinessPlatform.Api.Configuration;

namespace OpenBusinessPlatform.Api.Modules.Identity;

public sealed class BootstrapAdminUserDirectory
{
    public const string BootstrapAdminId = "bootstrap-admin";

    private readonly BootstrapAdminOptions options;

    public BootstrapAdminUserDirectory(IOptions<BootstrapAdminOptions> options)
    {
        this.options = options.Value;
    }

    public AuthenticatedUser? ValidateCredentials(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            return null;
        }

        var configuredEmail = NormalizeEmail(options.Email);
        var requestedEmail = NormalizeEmail(request.Email);

        if (!string.Equals(configuredEmail, requestedEmail, StringComparison.Ordinal))
        {
            return null;
        }

        if (!PasswordMatches(request.Password, options.Password))
        {
            return null;
        }

        return new AuthenticatedUser(
            BootstrapAdminId,
            "Platform Admin",
            configuredEmail,
            new[] { PlatformRoles.Admin });
    }

    private static string NormalizeEmail(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    private static bool PasswordMatches(string providedPassword, string configuredPassword)
    {
        var providedBytes = Encoding.UTF8.GetBytes(providedPassword);
        var configuredBytes = Encoding.UTF8.GetBytes(configuredPassword);

        return providedBytes.Length == configuredBytes.Length
            && CryptographicOperations.FixedTimeEquals(providedBytes, configuredBytes);
    }
}
