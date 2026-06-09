namespace OpenBusinessPlatform.Api.Modules.Integrations;

public sealed record CreateIntegrationApiKeyRequest(
    string Name,
    string IntegrationKey,
    IReadOnlyCollection<string>? Scopes = null,
    bool IsActive = true);

public sealed record RevokeIntegrationApiKeyRequest(
    string? Reason = null,
    string? ConcurrencyStamp = null);

public sealed record RotateIntegrationApiKeyRequest(
    string? ConcurrencyStamp = null);

public sealed record IntegrationApiKeyDto(
    Guid Id,
    string Name,
    string IntegrationKey,
    string KeyPrefix,
    IReadOnlyCollection<string> Scopes,
    bool IsActive,
    DateTimeOffset? LastUsedAt,
    string? LastUsedIp,
    string? LastUsedUserAgent,
    DateTimeOffset? RevokedAt,
    Guid? RevokedById,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById);

public sealed record CreatedIntegrationApiKeyDto(
    IntegrationApiKeyDto ApiKey,
    string RawKey);

public sealed record IntegrationApiKeyUsageContext(
    string? IpAddress = null,
    string? UserAgent = null);

public sealed record IntegrationApiKeyAuthenticationResult(
    bool Succeeded,
    IntegrationApiKeyDto? ApiKey,
    System.Security.Claims.ClaimsPrincipal? Principal,
    string? FailureReason);

public sealed record IntegrationApiKeyErrorResponse(string Message);

public sealed class IntegrationApiKeyException : Exception
{
    public IntegrationApiKeyException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
