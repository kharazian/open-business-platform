using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed class TriggerDefinitionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;

    public TriggerDefinitionService(OpenBusinessPlatformDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<TriggerSummaryDto>> ListTriggersAsync(Guid formId, CancellationToken cancellationToken)
    {
        var formExists = await dbContext.Forms
            .AsNoTracking()
            .AnyAsync(form => form.Id == formId && !form.IsDeleted, cancellationToken);

        if (!formExists)
        {
            throw new TriggerManagementException(StatusCodes.Status404NotFound, "Form was not found.");
        }

        var triggers = await dbContext.Triggers
            .AsNoTracking()
            .Where(trigger => trigger.FormId == formId && !trigger.IsDeleted)
            .OrderByDescending(trigger => trigger.UpdatedAt ?? trigger.CreatedAt)
            .ThenBy(trigger => trigger.Name)
            .ToArrayAsync(cancellationToken);

        return triggers.Select(ToSummaryDto).ToArray();
    }

    public async Task<TriggerDetailDto> CreateTriggerAsync(
        Guid formId,
        CreateTriggerRequest request,
        Guid? createdById,
        CancellationToken cancellationToken)
    {
        var form = await dbContext.Forms
            .Include(candidate => candidate.CurrentVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == formId && !candidate.IsDeleted, cancellationToken);

        if (form is null)
        {
            throw new TriggerManagementException(StatusCodes.Status404NotFound, "Form was not found.");
        }

        var schema = ResolveAuthoringSchema(form);

        if (schema is null)
        {
            throw new TriggerManagementException(StatusCodes.Status409Conflict, "Form schema is not available for trigger authoring.");
        }

        var activeUserIds = await GetActiveUserIdsAsync(cancellationToken);
        var activeGroupIds = await GetActiveGroupIdsAsync(cancellationToken);
        var validation = TriggerDefinitionValidator.Validate(schema, request, activeUserIds, activeGroupIds);

        if (!validation.Valid)
        {
            throw new TriggerManagementException(StatusCodes.Status400BadRequest, "Trigger definition is invalid.", validation.Errors);
        }

        var conditions = TriggerDefinitionValidator.NormalizeConditions(request.Conditions);
        var actions = TriggerDefinitionValidator.NormalizeActions(request.Actions);
        var trigger = new TriggerDefinition
        {
            Id = Guid.NewGuid(),
            FormId = form.Id,
            Form = form,
            Name = request.Name.Trim(),
            Description = NormalizeOptionalText(request.Description),
            EventName = request.EventName.Trim(),
            ConditionsJson = Serialize(conditions),
            ActionsJson = Serialize(actions),
            IsEnabled = request.IsEnabled,
            CreatedById = createdById
        };

        dbContext.Triggers.Add(trigger);
        AddAudit(trigger.Id, "trigger_created", createdById);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDetailDto(trigger);
    }

    public async Task<TriggerDetailDto> UpdateTriggerAsync(
        Guid triggerId,
        UpdateTriggerRequest request,
        Guid? updatedById,
        CancellationToken cancellationToken)
    {
        var trigger = await dbContext.Triggers
            .Include(candidate => candidate.Form)
            .ThenInclude(form => form!.CurrentVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == triggerId && !candidate.IsDeleted, cancellationToken);

        if (trigger is null || trigger.Form is null)
        {
            throw new TriggerManagementException(StatusCodes.Status404NotFound, "Trigger was not found.");
        }

        EnsureConcurrencyStamp(trigger.ConcurrencyStamp, request.ConcurrencyStamp);

        var schema = ResolveAuthoringSchema(trigger.Form);

        if (schema is null)
        {
            throw new TriggerManagementException(StatusCodes.Status409Conflict, "Form schema is not available for trigger authoring.");
        }

        var activeUserIds = await GetActiveUserIdsAsync(cancellationToken);
        var activeGroupIds = await GetActiveGroupIdsAsync(cancellationToken);
        var validation = TriggerDefinitionValidator.Validate(schema, request, activeUserIds, activeGroupIds);

        if (!validation.Valid)
        {
            throw new TriggerManagementException(StatusCodes.Status400BadRequest, "Trigger definition is invalid.", validation.Errors);
        }

        var wasEnabled = trigger.IsEnabled;
        trigger.Name = request.Name.Trim();
        trigger.Description = NormalizeOptionalText(request.Description);
        trigger.EventName = request.EventName.Trim();
        trigger.ConditionsJson = Serialize(TriggerDefinitionValidator.NormalizeConditions(request.Conditions));
        trigger.ActionsJson = Serialize(TriggerDefinitionValidator.NormalizeActions(request.Actions));
        trigger.IsEnabled = request.IsEnabled;
        trigger.UpdatedById = updatedById;

        AddAudit(trigger.Id, "trigger_updated", updatedById);

        if (wasEnabled && !trigger.IsEnabled)
        {
            AddAudit(trigger.Id, "trigger_disabled", updatedById);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDetailDto(trigger);
    }

    public async Task<TriggerDetailDto> GetTriggerAsync(Guid triggerId, CancellationToken cancellationToken)
    {
        var trigger = await dbContext.Triggers
            .AsNoTracking()
            .FirstOrDefaultAsync(candidate => candidate.Id == triggerId && !candidate.IsDeleted, cancellationToken);

        if (trigger is null)
        {
            throw new TriggerManagementException(StatusCodes.Status404NotFound, "Trigger was not found.");
        }

        return ToDetailDto(trigger);
    }

    public async Task<IReadOnlyCollection<TriggerExecutionLogDto>> ListTriggerLogsAsync(
        Guid triggerId,
        CancellationToken cancellationToken)
    {
        var triggerExists = await dbContext.Triggers
            .AsNoTracking()
            .AnyAsync(trigger => trigger.Id == triggerId && !trigger.IsDeleted, cancellationToken);

        if (!triggerExists)
        {
            throw new TriggerManagementException(StatusCodes.Status404NotFound, "Trigger was not found.");
        }

        var logs = await dbContext.TriggerLogs
            .AsNoTracking()
            .Where(log => log.TriggerId == triggerId)
            .OrderByDescending(log => log.CreatedAt)
            .ToArrayAsync(cancellationToken);

        return logs.Select(ToLogDto).ToArray();
    }

    public async Task<Guid?> GetTriggerFormIdAsync(Guid triggerId, CancellationToken cancellationToken)
    {
        return await dbContext.Triggers
            .AsNoTracking()
            .Where(trigger => trigger.Id == triggerId && !trigger.IsDeleted)
            .Select(trigger => (Guid?)trigger.FormId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<Guid>> GetActiveUserIdsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Users
            .AsNoTracking()
            .Where(user => user.IsActive)
            .Select(user => user.Id)
            .ToArrayAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<Guid>> GetActiveGroupIdsAsync(CancellationToken cancellationToken)
    {
        return await dbContext.Groups
            .AsNoTracking()
            .Where(group => group.IsActive)
            .Select(group => group.Id)
            .ToArrayAsync(cancellationToken);
    }

    private void AddAudit(Guid triggerId, string action, Guid? userId)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Trigger",
            EntityId = triggerId,
            Action = action,
            UserId = userId
        });
    }

    private static TriggerSummaryDto ToSummaryDto(TriggerDefinition trigger)
    {
        var conditions = DeserializeConditions(trigger.ConditionsJson);
        var actions = DeserializeActions(trigger.ActionsJson);

        return new TriggerSummaryDto(
            trigger.Id,
            trigger.FormId,
            trigger.Name,
            trigger.Description,
            trigger.EventName,
            trigger.IsEnabled,
            conditions.Conditions.Count,
            actions.Count,
            trigger.ConcurrencyStamp,
            trigger.CreatedAt,
            trigger.CreatedById,
            trigger.UpdatedAt,
            trigger.UpdatedById);
    }

    private static TriggerDetailDto ToDetailDto(TriggerDefinition trigger)
    {
        return new TriggerDetailDto(
            trigger.Id,
            trigger.FormId,
            trigger.Name,
            trigger.Description,
            trigger.EventName,
            DeserializeConditions(trigger.ConditionsJson),
            DeserializeActions(trigger.ActionsJson),
            trigger.IsEnabled,
            trigger.ConcurrencyStamp,
            trigger.CreatedAt,
            trigger.CreatedById,
            trigger.UpdatedAt,
            trigger.UpdatedById);
    }

    internal static TriggerExecutionLogDto ToLogDto(TriggerExecutionLog log)
    {
        return new TriggerExecutionLogDto(
            log.Id,
            log.TriggerId,
            log.FormId,
            log.EventName,
            log.EntityType,
            log.EntityId,
            log.Status,
            DeserializeObject(log.InputJson),
            DeserializeObject(log.ResultJson),
            log.ErrorMessage,
            log.StartedAt,
            log.CompletedAt,
            log.CreatedAt,
            ResolveRetrySourceLogId(log.InputJson, log.ResultJson));
    }

    private static void EnsureConcurrencyStamp(string currentStamp, string requestedStamp)
    {
        if (!string.Equals(currentStamp, requestedStamp, StringComparison.Ordinal))
        {
            throw new TriggerManagementException(StatusCodes.Status409Conflict, "The trigger was changed by another user.");
        }
    }

    private static FormSchemaDefinition? ResolveAuthoringSchema(FormDefinition form)
    {
        return DeserializeSchema(form.DraftSchemaJson) ?? DeserializeSchema(form.CurrentVersion?.SchemaJson);
    }

    private static JsonDocument Serialize<TValue>(TValue value)
    {
        return JsonSerializer.SerializeToDocument(value, JsonOptions);
    }

    private static TriggerConditionGroupDefinition DeserializeConditions(JsonDocument conditionsJson)
    {
        var conditions = conditionsJson.RootElement.Deserialize<TriggerConditionGroupDefinition>(JsonOptions);
        return TriggerDefinitionValidator.NormalizeConditions(conditions);
    }

    private static IReadOnlyList<TriggerActionDefinition> DeserializeActions(JsonDocument actionsJson)
    {
        var actions = actionsJson.RootElement.Deserialize<IReadOnlyList<TriggerActionDefinition>>(JsonOptions);
        return TriggerDefinitionValidator.NormalizeActions(actions);
    }

    private static object? DeserializeObject(JsonDocument? json)
    {
        return json is null
            ? null
            : JsonSerializer.Deserialize<object>(json.RootElement.GetRawText(), JsonOptions);
    }

    private static Guid? ResolveRetrySourceLogId(JsonDocument? inputJson, JsonDocument? resultJson)
    {
        return TryReadRetrySourceLogId(inputJson) ?? TryReadRetrySourceLogId(resultJson);
    }

    private static Guid? TryReadRetrySourceLogId(JsonDocument? json)
    {
        if (json is null
            || json.RootElement.ValueKind != JsonValueKind.Object
            || !json.RootElement.TryGetProperty("retry", out var retry)
            || retry.ValueKind != JsonValueKind.Object
            || !retry.TryGetProperty("sourceLogId", out var sourceLogId))
        {
            return null;
        }

        return sourceLogId.ValueKind == JsonValueKind.String && sourceLogId.TryGetGuid(out var id)
            ? id
            : null;
    }

    private static FormSchemaDefinition? DeserializeSchema(JsonDocument? schemaJson)
    {
        return schemaJson?.RootElement.Deserialize<FormSchemaDefinition>(JsonOptions);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var normalized = value?.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? null : normalized;
    }
}
