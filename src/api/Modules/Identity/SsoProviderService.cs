using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Workspaces;

namespace OpenBusinessPlatform.Api.Modules.Identity;

public sealed class SsoException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public sealed class SsoProviderService(
    OpenBusinessPlatformDbContext dbContext,
    IConfiguration configuration)
{
    public async Task<IReadOnlyCollection<SsoProviderDto>> ListAsync(CancellationToken cancellationToken)
    {
        var providers = await dbContext.SsoProviders.AsNoTracking()
            .OrderBy(provider => provider.DisplayName)
            .ToArrayAsync(cancellationToken);
        return providers.Select(ToDto).ToArray();
    }

    public async Task<IReadOnlyCollection<PublicSsoProviderDto>> ListPublicAsync(
        string tenantSlug,
        string workspaceSlug,
        CancellationToken cancellationToken)
    {
        var workspaceId = await ResolveActiveWorkspaceAsync(tenantSlug, workspaceSlug, cancellationToken);
        if (workspaceId is null)
        {
            return Array.Empty<PublicSsoProviderDto>();
        }

        return await dbContext.SsoProviders.IgnoreQueryFilters().AsNoTracking()
            .Where(provider => provider.WorkspaceId == workspaceId && provider.IsEnabled)
            .OrderBy(provider => provider.DisplayName)
            .Select(provider => new PublicSsoProviderDto(provider.Id, provider.ProviderKey, provider.DisplayName))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<SsoProviderDto> CreateAsync(
        SaveSsoProviderRequest request,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var values = Validate(request);
        if (await dbContext.SsoProviders.AnyAsync(provider => provider.ProviderKey == values.ProviderKey, cancellationToken))
        {
            throw new SsoException(StatusCodes.Status400BadRequest, "An SSO provider with this key already exists.");
        }

        var provider = new SsoProvider
        {
            Id = Guid.NewGuid(),
            ProviderKey = values.ProviderKey,
            DisplayName = values.DisplayName,
            Issuer = values.Issuer,
            ClientId = values.ClientId,
            ClientSecretConfigurationKey = values.ClientSecretConfigurationKey,
            CallbackUrl = values.CallbackUrl,
            IsEnabled = values.IsEnabled,
            CreatedById = actorUserId
        };
        dbContext.SsoProviders.Add(provider);
        AddAudit(provider.Id, "sso_provider_created", actorUserId, provider.ProviderKey, provider.IsEnabled);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(provider);
    }

    public async Task<SsoProviderDto?> UpdateAsync(
        Guid providerId,
        SaveSsoProviderRequest request,
        Guid? actorUserId,
        CancellationToken cancellationToken)
    {
        var provider = await dbContext.SsoProviders.SingleOrDefaultAsync(item => item.Id == providerId, cancellationToken);
        if (provider is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.ConcurrencyStamp)
            || !string.Equals(provider.ConcurrencyStamp, request.ConcurrencyStamp.Trim(), StringComparison.Ordinal))
        {
            throw new DbUpdateConcurrencyException("The SSO provider changed. Refresh and try again.");
        }

        var values = Validate(request);
        if (await dbContext.SsoProviders.AnyAsync(
                item => item.Id != providerId && item.ProviderKey == values.ProviderKey,
                cancellationToken))
        {
            throw new SsoException(StatusCodes.Status400BadRequest, "An SSO provider with this key already exists.");
        }

        provider.ProviderKey = values.ProviderKey;
        provider.DisplayName = values.DisplayName;
        provider.Issuer = values.Issuer;
        provider.ClientId = values.ClientId;
        provider.ClientSecretConfigurationKey = values.ClientSecretConfigurationKey;
        provider.CallbackUrl = values.CallbackUrl;
        provider.IsEnabled = values.IsEnabled;
        provider.UpdatedById = actorUserId;
        AddAudit(provider.Id, "sso_provider_updated", actorUserId, provider.ProviderKey, provider.IsEnabled);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(provider);
    }

    public async Task<(SsoProvider Provider, Guid WorkspaceId)> ResolveEnabledAsync(
        string tenantSlug,
        string workspaceSlug,
        string providerKey,
        CancellationToken cancellationToken)
    {
        var workspaceId = await ResolveActiveWorkspaceAsync(tenantSlug, workspaceSlug, cancellationToken)
            ?? throw new SsoException(StatusCodes.Status404NotFound, "Workspace SSO configuration was not found.");
        var normalizedKey = NormalizeProviderKey(providerKey);
        var provider = await dbContext.SsoProviders.IgnoreQueryFilters().AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.WorkspaceId == workspaceId
                && item.ProviderKey == normalizedKey
                && item.IsEnabled,
                cancellationToken)
            ?? throw new SsoException(StatusCodes.Status404NotFound, "Workspace SSO configuration was not found.");
        EnsureSecretConfigured(provider.ClientSecretConfigurationKey, true);
        return (provider, workspaceId);
    }

    public string GetClientSecret(SsoProvider provider)
    {
        return configuration[provider.ClientSecretConfigurationKey]
            ?? throw new SsoException(StatusCodes.Status503ServiceUnavailable, "The SSO provider secret is not configured.");
    }

    private async Task<Guid?> ResolveActiveWorkspaceAsync(
        string tenantSlug,
        string workspaceSlug,
        CancellationToken cancellationToken)
    {
        var tenant = NormalizeSlug(tenantSlug, "Tenant slug");
        var workspace = NormalizeSlug(workspaceSlug, "Workspace slug");
        return await dbContext.Workspaces.AsNoTracking()
            .Where(item => item.Slug == workspace && item.IsActive && item.Tenant != null && item.Tenant.IsActive && item.Tenant.Slug == tenant)
            .Select(item => (Guid?)item.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }

    private ValidatedProvider Validate(SaveSsoProviderRequest request)
    {
        var providerKey = NormalizeProviderKey(request.ProviderKey);
        var displayName = Required(request.DisplayName, "Display name", 120);
        var issuer = NormalizeHttpsUrl(request.Issuer, "Issuer").TrimEnd('/');
        var callbackUrl = NormalizeHttpsUrl(request.CallbackUrl, "Callback URL");
        var issuerUri = new Uri(issuer);
        var callbackUri = new Uri(callbackUrl);
        if (!string.IsNullOrEmpty(issuerUri.Query))
        {
            throw new SsoException(StatusCodes.Status400BadRequest, "Issuer must not contain a query string.");
        }
        if (!string.Equals(callbackUri.AbsolutePath, "/api/auth/sso/callback", StringComparison.Ordinal)
            || !string.IsNullOrEmpty(callbackUri.Query))
        {
            throw new SsoException(StatusCodes.Status400BadRequest, "Callback URL must use the fixed /api/auth/sso/callback path without a query string.");
        }
        var clientId = Required(request.ClientId, "Client ID", 300);
        var secretKey = Required(request.ClientSecretConfigurationKey, "Client secret configuration key", 200);
        if (secretKey.Any(character => !(char.IsLetterOrDigit(character) || "_:.-".Contains(character))))
        {
            throw new SsoException(StatusCodes.Status400BadRequest, "Client secret configuration key contains unsupported characters.");
        }
        EnsureSecretConfigured(secretKey, request.IsEnabled);
        return new(providerKey, displayName, issuer, clientId, secretKey, callbackUrl, request.IsEnabled);
    }

    private void EnsureSecretConfigured(string key, bool required)
    {
        if (required && string.IsNullOrWhiteSpace(configuration[key]))
        {
            throw new SsoException(StatusCodes.Status400BadRequest, "Configure the referenced client secret before enabling this provider.");
        }
    }

    private SsoProviderDto ToDto(SsoProvider provider) => new(
        provider.Id,
        provider.ProviderKey,
        provider.DisplayName,
        provider.Issuer,
        provider.ClientId,
        provider.ClientSecretConfigurationKey,
        !string.IsNullOrWhiteSpace(configuration[provider.ClientSecretConfigurationKey]),
        provider.CallbackUrl,
        provider.IsEnabled,
        provider.ConcurrencyStamp);

    private void AddAudit(Guid providerId, string action, Guid? actorUserId, string providerKey, bool isEnabled)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "SsoProvider",
            EntityId = providerId,
            Action = action,
            UserId = actorUserId,
            MetadataJson = JsonSerializer.SerializeToDocument(new { providerKey, isEnabled })
        });
    }

    private static string NormalizeProviderKey(string value)
    {
        var key = Required(value, "Provider key", 80).ToLowerInvariant();
        if (key.Any(character => !(char.IsLower(character) || char.IsDigit(character) || character is '-' or '_')))
        {
            throw new SsoException(StatusCodes.Status400BadRequest, "Provider key may contain lowercase letters, numbers, hyphens, and underscores only.");
        }
        return key;
    }

    private static string NormalizeSlug(string value, string label) => Required(value, label, 120).ToLowerInvariant();

    private static string NormalizeHttpsUrl(string value, string label)
    {
        var normalized = Required(value, label, 500);
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || !string.IsNullOrEmpty(uri.Fragment)
            || !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new SsoException(StatusCodes.Status400BadRequest, $"{label} must be an absolute HTTPS URL without credentials or a fragment.");
        }
        return uri.AbsoluteUri;
    }

    private static string Required(string? value, string label, int maxLength)
    {
        var normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length == 0 || normalized.Length > maxLength)
        {
            throw new SsoException(StatusCodes.Status400BadRequest, $"{label} is required and must be at most {maxLength} characters.");
        }
        return normalized;
    }

    private sealed record ValidatedProvider(
        string ProviderKey,
        string DisplayName,
        string Issuer,
        string ClientId,
        string ClientSecretConfigurationKey,
        string CallbackUrl,
        bool IsEnabled);
}
