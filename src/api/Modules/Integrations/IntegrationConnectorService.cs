using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;

namespace OpenBusinessPlatform.Api.Modules.Integrations;

public sealed class IntegrationConnectorService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;

    public IntegrationConnectorService(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<IntegrationConnectorDto>> ListAsync(CancellationToken cancellationToken)
    {
        var connectors = await dbContext.IntegrationConnectors
            .AsNoTracking()
            .OrderBy(connector => connector.Type)
            .ThenBy(connector => connector.Name)
            .ToArrayAsync(cancellationToken);

        return connectors.Select(ToDto).ToArray();
    }

    public async Task<IntegrationConnectorDto?> GetAsync(Guid connectorId, CancellationToken cancellationToken)
    {
        var connector = await dbContext.IntegrationConnectors
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == connectorId, cancellationToken);

        return connector is null ? null : ToDto(connector);
    }

    public async Task<IntegrationConnectorDto> CreateAsync(
        UpsertIntegrationConnectorRequest request,
        Guid? createdById,
        CancellationToken cancellationToken)
    {
        var name = NormalizeName(request.Name);
        var connectorKey = NormalizeConnectorKey(request.ConnectorKey);
        var type = NormalizeType(request.Type);
        await EnsureConnectorKeyIsUniqueAsync(connectorKey, null, cancellationToken);

        var connector = new IntegrationConnector
        {
            Id = Guid.NewGuid(),
            Name = name,
            ConnectorKey = connectorKey,
            Type = type,
            ConfigJson = SerializeConfig(request.Config),
            SecretMetadataJson = SerializeSecretNames(GetConfiguredSecretNames(request.Secrets)),
            IsActive = request.IsActive,
            CreatedById = createdById
        };

        dbContext.IntegrationConnectors.Add(connector);
        AddAudit(connector.Id, "integration_connector_created", createdById, new
        {
            connector.ConnectorKey,
            connector.Type,
            secretNames = DeserializeSecretNames(connector.SecretMetadataJson)
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(connector);
    }

    public async Task<IntegrationConnectorDto?> UpdateAsync(
        Guid connectorId,
        UpsertIntegrationConnectorRequest request,
        Guid? updatedById,
        CancellationToken cancellationToken)
    {
        var connector = await dbContext.IntegrationConnectors
            .SingleOrDefaultAsync(candidate => candidate.Id == connectorId, cancellationToken);

        if (connector is null)
        {
            return null;
        }

        EnsureConcurrencyStamp(connector.ConcurrencyStamp, request.ConcurrencyStamp);

        var name = NormalizeName(request.Name);
        var connectorKey = NormalizeConnectorKey(request.ConnectorKey);
        var type = NormalizeType(request.Type);
        await EnsureConnectorKeyIsUniqueAsync(connectorKey, connector.Id, cancellationToken);

        connector.Name = name;
        connector.ConnectorKey = connectorKey;
        connector.Type = type;
        connector.ConfigJson = SerializeConfig(request.Config);
        if (request.Secrets is not null)
        {
            connector.SecretMetadataJson = SerializeSecretNames(GetConfiguredSecretNames(request.Secrets));
        }

        connector.IsActive = request.IsActive;
        connector.UpdatedById = updatedById;
        connector.ConcurrencyStamp = Guid.NewGuid().ToString("N");

        AddAudit(connector.Id, "integration_connector_updated", updatedById, new
        {
            connector.ConnectorKey,
            connector.Type,
            secretNames = DeserializeSecretNames(connector.SecretMetadataJson)
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(connector);
    }

    private async Task EnsureConnectorKeyIsUniqueAsync(string connectorKey, Guid? currentConnectorId, CancellationToken cancellationToken)
    {
        var exists = currentConnectorId is null
            ? await dbContext.IntegrationConnectors.AnyAsync(connector => connector.ConnectorKey == connectorKey, cancellationToken)
            : await dbContext.IntegrationConnectors.AnyAsync(
                connector => connector.ConnectorKey == connectorKey && connector.Id != currentConnectorId.Value,
                cancellationToken);

        if (exists)
        {
            throw new IntegrationConnectorException(StatusCodes.Status409Conflict, "Connector key is already used.");
        }
    }

    private static IntegrationConnectorDto ToDto(IntegrationConnector connector)
    {
        return new IntegrationConnectorDto(
            connector.Id,
            connector.Name,
            connector.ConnectorKey,
            connector.Type,
            DeserializeConfig(connector.ConfigJson),
            DeserializeSecretNames(connector.SecretMetadataJson),
            connector.IsActive,
            connector.ConcurrencyStamp,
            connector.CreatedAt,
            connector.CreatedById,
            connector.UpdatedAt,
            connector.UpdatedById);
    }

    private static string NormalizeName(string? value)
    {
        var name = value?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new IntegrationConnectorException(StatusCodes.Status400BadRequest, "Connector name is required.");
        }

        return name.Length <= 160 ? name : name[..160];
    }

    private static string NormalizeConnectorKey(string? value)
    {
        var key = value?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new IntegrationConnectorException(StatusCodes.Status400BadRequest, "Connector key is required.");
        }

        if (key.Length > 120 || key.Any(character => !char.IsLetterOrDigit(character) && character is not '-' and not '_'))
        {
            throw new IntegrationConnectorException(StatusCodes.Status400BadRequest, "Connector key can include lowercase letters, numbers, hyphens, and underscores.");
        }

        return key;
    }

    private static string NormalizeType(string? value)
    {
        var type = value?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(type) || !IntegrationConnectorTypes.Supported.Contains(type))
        {
            throw new IntegrationConnectorException(StatusCodes.Status400BadRequest, "Connector type is not supported.");
        }

        return type;
    }

    private static JsonDocument SerializeConfig(IReadOnlyDictionary<string, object?>? config)
    {
        var sanitized = IntegrationMetadataSanitizer.Sanitize(config) ?? new Dictionary<string, object?>(StringComparer.Ordinal);
        return JsonSerializer.SerializeToDocument(sanitized, JsonOptions);
    }

    private static IReadOnlyList<string> GetConfiguredSecretNames(IReadOnlyDictionary<string, string?>? secrets)
    {
        if (secrets is null)
        {
            return Array.Empty<string>();
        }

        return secrets
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
            .Select(pair => pair.Key.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }

    private static JsonDocument SerializeSecretNames(IReadOnlyList<string> names)
    {
        return JsonSerializer.SerializeToDocument(names, JsonOptions);
    }

    private static IReadOnlyDictionary<string, object?> DeserializeConfig(JsonDocument config)
    {
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(config.RootElement.GetRawText(), JsonOptions)
            ?? new Dictionary<string, object?>(StringComparer.Ordinal);
    }

    private static IReadOnlyList<string> DeserializeSecretNames(JsonDocument secretMetadata)
    {
        return JsonSerializer.Deserialize<string[]>(secretMetadata.RootElement.GetRawText(), JsonOptions) ?? Array.Empty<string>();
    }

    private static void EnsureConcurrencyStamp(string currentStamp, string? requestedStamp)
    {
        if (!string.Equals(currentStamp, requestedStamp, StringComparison.Ordinal))
        {
            throw new IntegrationConnectorException(StatusCodes.Status409Conflict, "Connector was changed by another request.");
        }
    }

    private void AddAudit(Guid connectorId, string action, Guid? userId, object metadata)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "IntegrationConnector",
            EntityId = connectorId,
            Action = action,
            UserId = userId,
            MetadataJson = JsonSerializer.SerializeToDocument(metadata, JsonOptions)
        });
    }
}
