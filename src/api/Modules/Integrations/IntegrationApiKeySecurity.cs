using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using OpenBusinessPlatform.Api.Domain.Entities;

namespace OpenBusinessPlatform.Api.Modules.Integrations;

public static class IntegrationApiKeyScopes
{
    public const string Authenticate = "integrations.authenticate";
    public const string RecordsRead = "integrations.records.read";
    public const string RecordsCreate = "integrations.records.create";
    public const string WebhooksReceive = "integrations.webhooks.receive";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Authenticate,
        RecordsRead,
        RecordsCreate,
        WebhooksReceive
    };

    public static IReadOnlyCollection<string> Default { get; } = new[] { Authenticate };
}

public static class IntegrationApiKeyAuthenticationDefaults
{
    public const string AuthenticationScheme = "IntegrationApiKey";
}

public static class IntegrationApiKeyClaims
{
    public const string ApiKeyId = "obp.integration_api_key_id";
    public const string CreatedByUserId = "obp.integration_api_key_created_by_user_id";
    public const string IntegrationKey = "obp.integration_key";
    public const string Scope = "scope";
}

public sealed record GeneratedIntegrationApiKey(
    string RawKey,
    string KeyPrefix,
    string KeyHash);

public sealed class IntegrationApiKeyGenerator
{
    public const string RawKeyPrefix = "obp_sk_";

    private readonly IntegrationApiKeyHasher hasher;

    public IntegrationApiKeyGenerator(IntegrationApiKeyHasher hasher)
    {
        this.hasher = hasher;
    }

    public GeneratedIntegrationApiKey Generate()
    {
        var publicSegment = Encode(RandomNumberGenerator.GetBytes(12));
        var secretSegment = Encode(RandomNumberGenerator.GetBytes(32));
        var keyPrefix = $"{RawKeyPrefix}{publicSegment}";
        var rawKey = $"{keyPrefix}.{secretSegment}";

        return new GeneratedIntegrationApiKey(rawKey, keyPrefix, hasher.Hash(rawKey));
    }

    public static string? ExtractPrefix(string? rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return null;
        }

        var trimmed = rawKey.Trim();
        var separatorIndex = trimmed.IndexOf('.', StringComparison.Ordinal);

        if (separatorIndex <= 0)
        {
            return null;
        }

        var prefix = trimmed[..separatorIndex];
        return prefix.StartsWith(RawKeyPrefix, StringComparison.Ordinal) ? prefix : null;
    }

    private static string Encode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

public sealed class IntegrationApiKeyHasher
{
    public string Hash(string rawKey)
    {
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            throw new ArgumentException("API key is required.", nameof(rawKey));
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public bool Verify(string rawKey, string expectedHash)
    {
        if (string.IsNullOrWhiteSpace(rawKey) || string.IsNullOrWhiteSpace(expectedHash))
        {
            return false;
        }

        var actualHash = Hash(rawKey);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(actualHash),
            Encoding.UTF8.GetBytes(expectedHash));
    }
}

public static class IntegrationApiKeyAuthenticationPolicy
{
    public static bool CanAuthenticate(IntegrationApiKey apiKey)
    {
        return apiKey.IsActive && apiKey.RevokedAt is null;
    }
}

public static class IntegrationApiKeyPrincipalFactory
{
    public static ClaimsPrincipal Create(IntegrationApiKey apiKey, IReadOnlyCollection<string> scopes)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, apiKey.Id.ToString()),
            new(ClaimTypes.Name, apiKey.Name),
            new(IntegrationApiKeyClaims.ApiKeyId, apiKey.Id.ToString()),
            new(IntegrationApiKeyClaims.IntegrationKey, apiKey.IntegrationKey)
        };

        if (apiKey.CreatedById is not null)
        {
            claims.Add(new Claim(IntegrationApiKeyClaims.CreatedByUserId, apiKey.CreatedById.Value.ToString()));
        }

        claims.AddRange(scopes.Select(scope => new Claim(IntegrationApiKeyClaims.Scope, scope)));

        var identity = new ClaimsIdentity(claims, IntegrationApiKeyAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
