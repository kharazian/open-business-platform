using System.Security.Claims;

namespace OpenBusinessPlatform.Api.Modules.Identity;

public sealed record SsoProviderDto(
    Guid Id,
    string ProviderKey,
    string DisplayName,
    string Issuer,
    string ClientId,
    string ClientSecretConfigurationKey,
    bool IsClientSecretConfigured,
    string CallbackUrl,
    bool IsEnabled,
    string ConcurrencyStamp);

public sealed record PublicSsoProviderDto(Guid Id, string ProviderKey, string DisplayName);

public sealed record SaveSsoProviderRequest(
    string ProviderKey,
    string DisplayName,
    string Issuer,
    string ClientId,
    string ClientSecretConfigurationKey,
    string CallbackUrl,
    bool IsEnabled,
    string? ConcurrencyStamp);

public sealed record StartSsoRequest(
    string TenantSlug,
    string WorkspaceSlug,
    string ProviderKey,
    string? ReturnPath);

public sealed record StartSsoResponse(string AuthorizationUrl);

public sealed record SsoFlowState(
    Guid WorkspaceId,
    Guid ProviderId,
    string Nonce,
    string CodeVerifier,
    string ReturnPath,
    long IssuedAtUnixSeconds);

public sealed record SsoCallbackResult(ClaimsPrincipal Principal, string ReturnPath);
