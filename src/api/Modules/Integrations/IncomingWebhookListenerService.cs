using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Integrations;

public sealed class IncomingWebhookListenerService
{
    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly IncomingWebhookListenerSecretGenerator secretGenerator;

    public IncomingWebhookListenerService(
        OpenBusinessPlatformDbContext dbContext,
        IncomingWebhookListenerSecretGenerator secretGenerator)
    {
        this.dbContext = dbContext;
        this.secretGenerator = secretGenerator;
    }

    public async Task<IReadOnlyCollection<IncomingWebhookListenerDto>> ListAsync(CancellationToken cancellationToken)
    {
        var listeners = await dbContext.IncomingWebhookListeners
            .AsNoTracking()
            .OrderBy(listener => listener.Name)
            .ToArrayAsync(cancellationToken);

        return listeners.Select(ToDto).ToArray();
    }

    public async Task<IncomingWebhookListenerDto?> GetAsync(Guid listenerId, CancellationToken cancellationToken)
    {
        var listener = await dbContext.IncomingWebhookListeners
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == listenerId, cancellationToken);

        return listener is null ? null : ToDto(listener);
    }

    public async Task<IncomingWebhookListenerSecretDto> CreateAsync(
        UpsertIncomingWebhookListenerRequest request,
        Guid? createdById,
        CancellationToken cancellationToken)
    {
        var form = await GetPublishedTargetFormAsync(request.TargetFormId, cancellationToken);
        var normalized = Normalize(request, form.Schema);
        var generated = secretGenerator.Generate();
        var listener = new IncomingWebhookListener
        {
            Id = Guid.NewGuid(),
            Name = normalized.Name,
            ListenerKey = normalized.ListenerKey,
            TargetFormId = normalized.TargetFormId,
            Action = normalized.Action,
            AuthMode = normalized.AuthMode,
            SecretPrefix = generated.SecretPrefix,
            SecretHash = generated.SecretHash,
            SafeLookupFieldId = normalized.SafeLookupFieldId,
            MappingJson = Serialize(normalized.Mapping),
            IsActive = normalized.IsActive,
            CreatedById = createdById
        };

        dbContext.IncomingWebhookListeners.Add(listener);
        AddAudit(listener.Id, "incoming_webhook_listener_created", createdById, new
        {
            listener.ListenerKey,
            listener.TargetFormId,
            listener.Action,
            listener.AuthMode
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new IncomingWebhookListenerSecretDto(ToDto(listener), generated.RawSecret);
    }

    public async Task<IncomingWebhookListenerDto?> UpdateAsync(
        Guid listenerId,
        UpsertIncomingWebhookListenerRequest request,
        Guid? updatedById,
        CancellationToken cancellationToken)
    {
        var listener = await dbContext.IncomingWebhookListeners
            .SingleOrDefaultAsync(candidate => candidate.Id == listenerId, cancellationToken);

        if (listener is null)
        {
            return null;
        }

        var form = await GetPublishedTargetFormAsync(request.TargetFormId, cancellationToken);
        var normalized = Normalize(request, form.Schema);
        listener.Name = normalized.Name;
        listener.ListenerKey = normalized.ListenerKey;
        listener.TargetFormId = normalized.TargetFormId;
        listener.Action = normalized.Action;
        listener.AuthMode = normalized.AuthMode;
        listener.SafeLookupFieldId = normalized.SafeLookupFieldId;
        listener.MappingJson = Serialize(normalized.Mapping);
        listener.IsActive = normalized.IsActive;
        listener.UpdatedById = updatedById;

        AddAudit(listener.Id, "incoming_webhook_listener_updated", updatedById, new
        {
            listener.ListenerKey,
            listener.TargetFormId,
            listener.Action,
            listener.AuthMode
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(listener);
    }

    public async Task<IncomingWebhookListenerSecretDto?> RotateSecretAsync(
        Guid listenerId,
        Guid? updatedById,
        CancellationToken cancellationToken)
    {
        var listener = await dbContext.IncomingWebhookListeners
            .SingleOrDefaultAsync(candidate => candidate.Id == listenerId, cancellationToken);

        if (listener is null)
        {
            return null;
        }

        var previousPrefix = listener.SecretPrefix;
        var generated = secretGenerator.Generate();
        listener.SecretPrefix = generated.SecretPrefix;
        listener.SecretHash = generated.SecretHash;
        listener.UpdatedById = updatedById;

        AddAudit(listener.Id, "incoming_webhook_listener_secret_rotated", updatedById, new
        {
            listener.ListenerKey,
            previousPrefix,
            listener.SecretPrefix
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        return new IncomingWebhookListenerSecretDto(ToDto(listener), generated.RawSecret);
    }

    internal static IncomingWebhookListenerDto ToDto(IncomingWebhookListener listener)
    {
        return new IncomingWebhookListenerDto(
            listener.Id,
            listener.Name,
            listener.ListenerKey,
            listener.TargetFormId,
            listener.Action,
            listener.AuthMode,
            listener.SecretPrefix,
            listener.SafeLookupFieldId,
            DeserializeMapping(listener.MappingJson),
            listener.IsActive,
            listener.ConcurrencyStamp,
            listener.CreatedAt,
            listener.CreatedById,
            listener.UpdatedAt,
            listener.UpdatedById);
    }

    internal static IncomingWebhookMappingDefinition DeserializeMapping(JsonDocument mappingJson)
    {
        return mappingJson.RootElement.Deserialize<IncomingWebhookMappingDefinition>(IncomingWebhookJson.Options)
            ?? new IncomingWebhookMappingDefinition(Array.Empty<IncomingWebhookFieldMappingDefinition>());
    }

    private async Task<TargetFormSchema> GetPublishedTargetFormAsync(Guid formId, CancellationToken cancellationToken)
    {
        var form = await dbContext.Forms
            .AsNoTracking()
            .Include(candidate => candidate.CurrentVersion)
            .SingleOrDefaultAsync(candidate => candidate.Id == formId && !candidate.IsDeleted, cancellationToken);

        if (form?.CurrentVersion?.SchemaJson is null)
        {
            throw new IncomingWebhookException(StatusCodes.Status404NotFound, "Target form was not found.");
        }

        if (!string.Equals(form.Status, FormStatuses.Published, StringComparison.Ordinal)
            || form.CurrentVersionId is null)
        {
            throw new IncomingWebhookException(StatusCodes.Status409Conflict, "Incoming webhook listeners require a published target form.");
        }

        var schema = form.CurrentVersion.SchemaJson.RootElement.Deserialize<FormSchemaDefinition>(IncomingWebhookJson.Options);
        if (schema is null)
        {
            throw new IncomingWebhookException(StatusCodes.Status409Conflict, "Target form schema is invalid.");
        }

        return new TargetFormSchema(schema);
    }

    private static UpsertIncomingWebhookListenerRequest Normalize(
        UpsertIncomingWebhookListenerRequest request,
        FormSchemaDefinition schema)
    {
        var normalized = request with
        {
            Name = request.Name.Trim(),
            ListenerKey = request.ListenerKey.Trim().ToLowerInvariant(),
            Action = request.Action.Trim().ToLowerInvariant(),
            AuthMode = request.AuthMode.Trim().ToLowerInvariant(),
            SafeLookupFieldId = string.IsNullOrWhiteSpace(request.SafeLookupFieldId) ? null : request.SafeLookupFieldId.Trim(),
            Mapping = new IncomingWebhookMappingDefinition(request.Mapping.FieldMappings.Select(mapping => mapping with
            {
                SourcePath = mapping.SourcePath.Trim(),
                TargetFieldId = mapping.TargetFieldId.Trim()
            }).ToArray())
        };
        var validation = IncomingWebhookListenerValidator.Validate(normalized, schema);
        if (!validation.Valid)
        {
            throw new IncomingWebhookException(StatusCodes.Status400BadRequest, "Incoming webhook listener is invalid.");
        }

        return normalized;
    }

    private static JsonDocument Serialize(IncomingWebhookMappingDefinition mapping)
    {
        return JsonSerializer.SerializeToDocument(mapping, IncomingWebhookJson.Options);
    }

    private void AddAudit(Guid entityId, string action, Guid? userId, object metadata)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "IncomingWebhookListener",
            EntityId = entityId,
            Action = action,
            UserId = userId,
            MetadataJson = JsonSerializer.SerializeToDocument(metadata, IncomingWebhookJson.Options)
        });
    }

    private sealed record TargetFormSchema(FormSchemaDefinition Schema);
}
