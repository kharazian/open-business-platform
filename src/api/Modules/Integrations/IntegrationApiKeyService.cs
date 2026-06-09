using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Integrations;

public sealed class IntegrationApiKeyService
{
    private const int MaxGenerateAttempts = 5;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly IntegrationApiKeyGenerator generator;
    private readonly IntegrationApiKeyHasher hasher;

    public IntegrationApiKeyService(
        OpenBusinessPlatformDbContext dbContext,
        IntegrationApiKeyGenerator generator,
        IntegrationApiKeyHasher hasher)
    {
        this.dbContext = dbContext;
        this.generator = generator;
        this.hasher = hasher;
    }

    public async Task<IReadOnlyCollection<IntegrationApiKeyDto>> ListAsync(CancellationToken cancellationToken)
    {
        var apiKeys = await dbContext.IntegrationApiKeys
            .AsNoTracking()
            .OrderBy(apiKey => apiKey.IntegrationKey)
            .ThenBy(apiKey => apiKey.Name)
            .ToArrayAsync(cancellationToken);

        return apiKeys.Select(ToDto).ToArray();
    }

    public async Task<IntegrationApiKeyDto?> GetAsync(Guid apiKeyId, CancellationToken cancellationToken)
    {
        var apiKey = await dbContext.IntegrationApiKeys
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == apiKeyId, cancellationToken);

        return apiKey is null ? null : ToDto(apiKey);
    }

    public async Task<CreatedIntegrationApiKeyDto> CreateAsync(
        CreateIntegrationApiKeyRequest request,
        Guid? createdById,
        CancellationToken cancellationToken)
    {
        var name = NormalizeName(request.Name);
        var integrationKey = NormalizeIntegrationKey(request.IntegrationKey);
        var scopes = NormalizeScopes(request.Scopes);
        var generated = await GenerateUniqueKeyAsync(cancellationToken);

        var apiKey = new IntegrationApiKey
        {
            Id = Guid.NewGuid(),
            Name = name,
            IntegrationKey = integrationKey,
            KeyPrefix = generated.KeyPrefix,
            KeyHash = generated.KeyHash,
            ScopesJson = Serialize(scopes),
            IsActive = request.IsActive,
            CreatedById = createdById
        };

        dbContext.IntegrationApiKeys.Add(apiKey);
        AddAudit(apiKey.Id, "integration_api_key_created", createdById, new
        {
            apiKey.IntegrationKey,
            apiKey.KeyPrefix,
            scopes
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreatedIntegrationApiKeyDto(ToDto(apiKey), generated.RawKey);
    }

    public async Task<IntegrationApiKeyDto?> RevokeAsync(
        Guid apiKeyId,
        RevokeIntegrationApiKeyRequest request,
        Guid? revokedById,
        CancellationToken cancellationToken)
    {
        var apiKey = await dbContext.IntegrationApiKeys
            .SingleOrDefaultAsync(candidate => candidate.Id == apiKeyId, cancellationToken);

        if (apiKey is null)
        {
            return null;
        }

        EnsureConcurrencyStamp(apiKey.ConcurrencyStamp, request.ConcurrencyStamp);

        if (apiKey.RevokedAt is null)
        {
            apiKey.IsActive = false;
            apiKey.RevokedAt = DateTimeOffset.UtcNow;
            apiKey.RevokedById = revokedById;
            apiKey.UpdatedById = revokedById;
            AddAudit(apiKey.Id, "integration_api_key_revoked", revokedById, new
            {
                apiKey.IntegrationKey,
                apiKey.KeyPrefix,
                reason = NormalizeOptionalText(request.Reason, 500)
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return ToDto(apiKey);
    }

    public async Task<CreatedIntegrationApiKeyDto?> RotateAsync(
        Guid apiKeyId,
        RotateIntegrationApiKeyRequest request,
        Guid? rotatedById,
        CancellationToken cancellationToken)
    {
        var apiKey = await dbContext.IntegrationApiKeys
            .SingleOrDefaultAsync(candidate => candidate.Id == apiKeyId, cancellationToken);

        if (apiKey is null)
        {
            return null;
        }

        EnsureConcurrencyStamp(apiKey.ConcurrencyStamp, request.ConcurrencyStamp);

        if (!IntegrationApiKeyAuthenticationPolicy.CanAuthenticate(apiKey))
        {
            throw new IntegrationApiKeyException(StatusCodes.Status400BadRequest, "Only active, non-revoked API keys can be rotated.");
        }

        var previousPrefix = apiKey.KeyPrefix;
        var generated = await GenerateUniqueKeyAsync(cancellationToken);
        apiKey.KeyPrefix = generated.KeyPrefix;
        apiKey.KeyHash = generated.KeyHash;
        apiKey.LastUsedAt = null;
        apiKey.LastUsedIp = null;
        apiKey.LastUsedUserAgent = null;
        apiKey.UpdatedById = rotatedById;

        AddAudit(apiKey.Id, "integration_api_key_rotated", rotatedById, new
        {
            apiKey.IntegrationKey,
            previousPrefix,
            apiKey.KeyPrefix
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreatedIntegrationApiKeyDto(ToDto(apiKey), generated.RawKey);
    }

    public async Task<IntegrationApiKeyAuthenticationResult> AuthenticateAsync(
        string rawKey,
        IntegrationApiKeyUsageContext usageContext,
        CancellationToken cancellationToken)
    {
        var keyPrefix = IntegrationApiKeyGenerator.ExtractPrefix(rawKey);

        if (string.IsNullOrWhiteSpace(keyPrefix))
        {
            return Failed("API key format is invalid.");
        }

        var apiKey = await dbContext.IntegrationApiKeys
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.KeyPrefix == keyPrefix, cancellationToken);

        if (apiKey is null || !hasher.Verify(rawKey, apiKey.KeyHash))
        {
            return Failed("API key was not found.");
        }

        if (!IntegrationApiKeyAuthenticationPolicy.CanAuthenticate(apiKey))
        {
            return Failed("API key is inactive or revoked.");
        }

        var now = DateTimeOffset.UtcNow;
        var lastUsedIp = NormalizeOptionalText(usageContext.IpAddress, 80);
        var lastUsedUserAgent = NormalizeOptionalText(usageContext.UserAgent, 500);
        var updatedRows = await dbContext.IntegrationApiKeys
            .Where(candidate =>
                candidate.Id == apiKey.Id
                && candidate.KeyHash == apiKey.KeyHash
                && candidate.IsActive
                && candidate.RevokedAt == null)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(candidate => candidate.LastUsedAt, now)
                    .SetProperty(candidate => candidate.LastUsedIp, lastUsedIp)
                    .SetProperty(candidate => candidate.LastUsedUserAgent, lastUsedUserAgent),
                cancellationToken);

        if (updatedRows == 0)
        {
            return Failed("API key is inactive or revoked.");
        }

        var scopes = DeserializeScopes(apiKey.ScopesJson);
        var dto = ToDto(WithLastUsed(apiKey, now, lastUsedIp, lastUsedUserAgent));
        var principal = IntegrationApiKeyPrincipalFactory.Create(apiKey, scopes);

        return new IntegrationApiKeyAuthenticationResult(true, dto, principal, null);
    }

    private async Task<GeneratedIntegrationApiKey> GenerateUniqueKeyAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxGenerateAttempts; attempt++)
        {
            var generated = generator.Generate();
            var exists = await dbContext.IntegrationApiKeys.AnyAsync(
                apiKey => apiKey.KeyPrefix == generated.KeyPrefix || apiKey.KeyHash == generated.KeyHash,
                cancellationToken);

            if (!exists)
            {
                return generated;
            }
        }

        throw new IntegrationApiKeyException(StatusCodes.Status500InternalServerError, "Could not generate a unique API key.");
    }

    private static IntegrationApiKeyDto ToDto(IntegrationApiKey apiKey)
    {
        return new IntegrationApiKeyDto(
            apiKey.Id,
            apiKey.Name,
            apiKey.IntegrationKey,
            apiKey.KeyPrefix,
            DeserializeScopes(apiKey.ScopesJson),
            apiKey.IsActive,
            apiKey.LastUsedAt,
            apiKey.LastUsedIp,
            apiKey.LastUsedUserAgent,
            apiKey.RevokedAt,
            apiKey.RevokedById,
            apiKey.ConcurrencyStamp,
            apiKey.CreatedAt,
            apiKey.CreatedById,
            apiKey.UpdatedAt,
            apiKey.UpdatedById);
    }

    private static IReadOnlyCollection<string> NormalizeScopes(IReadOnlyCollection<string>? scopes)
    {
        var normalized = (scopes is null || scopes.Count == 0 ? IntegrationApiKeyScopes.Default : scopes)
            .Select(scope => scope?.Trim() ?? string.Empty)
            .Where(scope => !string.IsNullOrWhiteSpace(scope))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalized.Length == 0)
        {
            normalized = IntegrationApiKeyScopes.Default.ToArray();
        }

        var unsupported = normalized
            .Where(scope => !IntegrationApiKeyScopes.Supported.Contains(scope))
            .ToArray();

        if (unsupported.Length > 0)
        {
            throw new IntegrationApiKeyException(
                StatusCodes.Status400BadRequest,
                $"Unsupported integration API key scope: {unsupported[0]}.");
        }

        return normalized;
    }

    private static IReadOnlyCollection<string> DeserializeScopes(JsonDocument scopesJson)
    {
        return scopesJson.RootElement.Deserialize<string[]>(JsonOptions)
            ?? Array.Empty<string>();
    }

    private static JsonDocument Serialize<T>(T value)
    {
        return JsonSerializer.SerializeToDocument(value, JsonOptions);
    }

    private static string NormalizeName(string? value)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new IntegrationApiKeyException(StatusCodes.Status400BadRequest, "API key name is required.");
        }

        if (normalized.Length > 200)
        {
            throw new IntegrationApiKeyException(StatusCodes.Status400BadRequest, "API key name must be 200 characters or fewer.");
        }

        return normalized;
    }

    private static string NormalizeIntegrationKey(string? value)
    {
        var normalized = value?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new IntegrationApiKeyException(StatusCodes.Status400BadRequest, "Integration key is required.");
        }

        if (normalized.Length is < 2 or > 120
            || !IsLetterOrDigit(normalized[0])
            || normalized.Any(character => !IsIntegrationKeyCharacter(character)))
        {
            throw new IntegrationApiKeyException(
                StatusCodes.Status400BadRequest,
                "Integration key must start with a letter or number and use only lowercase letters, numbers, dots, dashes, or underscores.");
        }

        return normalized;
    }

    private static string? NormalizeOptionalText(string? value, int maxLength)
    {
        var normalized = value?.Trim();

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static bool IsIntegrationKeyCharacter(char character)
    {
        return IsLetterOrDigit(character) || character is '.' or '-' or '_';
    }

    private static bool IsLetterOrDigit(char character)
    {
        return character is >= 'a' and <= 'z' or >= '0' and <= '9';
    }

    private static void EnsureConcurrencyStamp(string current, string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested) && !string.Equals(current, requested, StringComparison.Ordinal))
        {
            throw new IntegrationApiKeyException(StatusCodes.Status409Conflict, "API key was updated by someone else. Refresh and try again.");
        }
    }

    private static IntegrationApiKey WithLastUsed(IntegrationApiKey apiKey, DateTimeOffset lastUsedAt, string? lastUsedIp, string? lastUsedUserAgent)
    {
        return new IntegrationApiKey
        {
            Id = apiKey.Id,
            Name = apiKey.Name,
            IntegrationKey = apiKey.IntegrationKey,
            KeyPrefix = apiKey.KeyPrefix,
            KeyHash = apiKey.KeyHash,
            ScopesJson = apiKey.ScopesJson,
            IsActive = apiKey.IsActive,
            LastUsedAt = lastUsedAt,
            LastUsedIp = lastUsedIp,
            LastUsedUserAgent = lastUsedUserAgent,
            RevokedAt = apiKey.RevokedAt,
            RevokedById = apiKey.RevokedById,
            ConcurrencyStamp = apiKey.ConcurrencyStamp,
            ExtraPropertiesJson = apiKey.ExtraPropertiesJson,
            CreatedAt = apiKey.CreatedAt,
            CreatedById = apiKey.CreatedById,
            UpdatedAt = apiKey.UpdatedAt,
            UpdatedById = apiKey.UpdatedById
        };
    }

    private void AddAudit(Guid entityId, string action, Guid? userId, object? metadata = null)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "IntegrationApiKey",
            EntityId = entityId,
            Action = action,
            UserId = userId,
            MetadataJson = metadata is null ? null : Serialize(metadata)
        });
    }

    private static IntegrationApiKeyAuthenticationResult Failed(string reason)
    {
        return new IntegrationApiKeyAuthenticationResult(false, null, null, reason);
    }
}
