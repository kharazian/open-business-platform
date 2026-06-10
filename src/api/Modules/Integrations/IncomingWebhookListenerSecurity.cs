using System.Security.Cryptography;
using System.Text;

namespace OpenBusinessPlatform.Api.Modules.Integrations;

public sealed record GeneratedIncomingWebhookListenerSecret(
    string RawSecret,
    string SecretPrefix,
    string SecretHash);

public sealed class IncomingWebhookListenerSecretGenerator
{
    public const string RawSecretPrefix = "obp_wh_";

    private readonly IncomingWebhookListenerSecretHasher hasher;

    public IncomingWebhookListenerSecretGenerator(IncomingWebhookListenerSecretHasher hasher)
    {
        this.hasher = hasher;
    }

    public GeneratedIncomingWebhookListenerSecret Generate()
    {
        var publicSegment = Encode(RandomNumberGenerator.GetBytes(10));
        var secretSegment = Encode(RandomNumberGenerator.GetBytes(32));
        var secretPrefix = $"{RawSecretPrefix}{publicSegment}";
        var rawSecret = $"{secretPrefix}.{secretSegment}";

        return new GeneratedIncomingWebhookListenerSecret(rawSecret, secretPrefix, hasher.Hash(rawSecret));
    }

    public static string? ExtractPrefix(string? rawSecret)
    {
        if (string.IsNullOrWhiteSpace(rawSecret))
        {
            return null;
        }

        var trimmed = rawSecret.Trim();
        var separatorIndex = trimmed.IndexOf('.', StringComparison.Ordinal);

        if (separatorIndex <= 0)
        {
            return null;
        }

        var prefix = trimmed[..separatorIndex];
        return prefix.StartsWith(RawSecretPrefix, StringComparison.Ordinal) ? prefix : null;
    }

    private static string Encode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}

public sealed class IncomingWebhookListenerSecretHasher
{
    public string Hash(string rawSecret)
    {
        if (string.IsNullOrWhiteSpace(rawSecret))
        {
            throw new ArgumentException("Webhook listener secret is required.", nameof(rawSecret));
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawSecret.Trim()));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    public bool Verify(string rawSecret, string expectedHash)
    {
        if (string.IsNullOrWhiteSpace(rawSecret) || string.IsNullOrWhiteSpace(expectedHash))
        {
            return false;
        }

        var actualHash = Hash(rawSecret);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(actualHash),
            Encoding.UTF8.GetBytes(expectedHash));
    }
}
