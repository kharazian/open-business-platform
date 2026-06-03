using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Notifications;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed class TriggerActionRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly IEmailSender emailSender;

    public TriggerActionRegistry(OpenBusinessPlatformDbContext dbContext, IEmailSender emailSender)
    {
        this.dbContext = dbContext;
        this.emailSender = emailSender;
    }

    public async Task<IReadOnlyDictionary<string, object?>> ExecuteAsync(
        TriggerActionDefinition action,
        TriggerEventContext context,
        Guid triggerId,
        Guid? triggerLogId,
        CancellationToken cancellationToken)
    {
        return action.Type switch
        {
            TriggerActionTypes.WriteAuditEntry => ExecuteWriteAuditEntry(action, context, triggerId, triggerLogId),
            TriggerActionTypes.SendEmail => await ExecuteSendEmailAsync(action, cancellationToken),
            TriggerActionTypes.ChangeStatus => await ExecuteChangeStatusAsync(action, context, triggerId, triggerLogId, cancellationToken),
            TriggerActionTypes.AssignRecord => await ExecuteAssignRecordAsync(action, context, triggerId, triggerLogId, cancellationToken),
            TriggerActionTypes.UpdateField => await ExecuteUpdateFieldAsync(action, context, triggerId, triggerLogId, cancellationToken),
            TriggerActionTypes.SendNotification => await ExecuteSendNotificationAsync(action, context, triggerId, triggerLogId, cancellationToken),
            _ => throw new InvalidOperationException($"Trigger action type '{action.Type}' is not supported.")
        };
    }

    private IReadOnlyDictionary<string, object?> ExecuteWriteAuditEntry(
        TriggerActionDefinition action,
        TriggerEventContext context,
        Guid triggerId,
        Guid? triggerLogId)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Record",
            EntityId = context.RecordId,
            Action = "trigger_audit_entry",
            UserId = context.ActorUserId,
            MetadataJson = BuildMetadata(triggerId, triggerLogId, action.Id, action.Message)
        });

        return Result(action, ("message", action.Message));
    }

    private async Task<IReadOnlyDictionary<string, object?>> ExecuteSendEmailAsync(
        TriggerActionDefinition action,
        CancellationToken cancellationToken)
    {
        var recipients = action.To ?? Array.Empty<string>();

        foreach (var recipient in recipients)
        {
            await emailSender.SendAsync(
                new EmailMessage(
                    recipient,
                    action.Subject ?? string.Empty,
                    action.Body ?? string.Empty),
                cancellationToken);
        }

        return Result(action, ("recipientCount", recipients.Count));
    }

    private async Task<IReadOnlyDictionary<string, object?>> ExecuteChangeStatusAsync(
        TriggerActionDefinition action,
        TriggerEventContext context,
        Guid triggerId,
        Guid? triggerLogId,
        CancellationToken cancellationToken)
    {
        var record = await LoadRecordAsync(context.RecordId, cancellationToken);
        var previousStatus = record.Status;
        record.Status = action.Status ?? string.Empty;
        record.UpdatedById = context.ActorUserId;
        AddRecordAudit(record.Id, "record_status_changed", context.ActorUserId, triggerId, triggerLogId, action.Id, previousStatus, record.Status);

        return Result(action, ("previousStatus", previousStatus), ("status", record.Status));
    }

    private async Task<IReadOnlyDictionary<string, object?>> ExecuteAssignRecordAsync(
        TriggerActionDefinition action,
        TriggerEventContext context,
        Guid triggerId,
        Guid? triggerLogId,
        CancellationToken cancellationToken)
    {
        var record = await LoadRecordAsync(context.RecordId, cancellationToken);
        var previousAssignedToUserId = record.AssignedToUserId;
        var previousAssignedGroupId = record.AssignedGroupId;
        record.AssignedToUserId = NormalizeNullableId(action.AssignedToUserId);
        record.AssignedGroupId = NormalizeNullableId(action.AssignedGroupId);
        record.UpdatedById = context.ActorUserId;
        AddRecordAudit(record.Id, "record_assigned", context.ActorUserId, triggerId, triggerLogId, action.Id, previousAssignedToUserId, previousAssignedGroupId);

        return Result(
            action,
            ("assignedToUserId", record.AssignedToUserId),
            ("assignedGroupId", record.AssignedGroupId));
    }

    private async Task<IReadOnlyDictionary<string, object?>> ExecuteUpdateFieldAsync(
        TriggerActionDefinition action,
        TriggerEventContext context,
        Guid triggerId,
        Guid? triggerLogId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(action.FieldId))
        {
            throw new InvalidOperationException("Trigger update field action field id is required.");
        }

        var record = await LoadRecordWithVersionAsync(context.RecordId, cancellationToken);
        var schema = record.FormVersion?.SchemaJson.RootElement.Deserialize<FormSchemaDefinition>(JsonOptions)
            ?? throw new InvalidOperationException("Trigger action record form schema was not found.");
        var currentValues = DeserializeValues(record.ValuesJson);
        currentValues.TryGetValue(action.FieldId, out var previousValue);

        var updatedValues = currentValues.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        updatedValues[action.FieldId] = action.Value;
        var validation = FormSchemaValidator.ValidateRecordValues(schema, updatedValues);

        if (!validation.Valid)
        {
            throw new InvalidOperationException(string.Join(" ", validation.Errors.Select(error => error.Message)));
        }

        record.ValuesJson = JsonSerializer.SerializeToDocument(updatedValues, JsonOptions);
        record.UpdatedById = context.ActorUserId;
        AddRecordAudit(record.Id, "record_field_updated", context.ActorUserId, triggerId, triggerLogId, action.Id, previousValue, action.Value);

        return Result(action, ("fieldId", action.FieldId), ("previousValue", previousValue), ("value", action.Value));
    }

    private async Task<IReadOnlyDictionary<string, object?>> ExecuteSendNotificationAsync(
        TriggerActionDefinition action,
        TriggerEventContext context,
        Guid triggerId,
        Guid? triggerLogId,
        CancellationToken cancellationToken)
    {
        var recipients = await ResolveNotificationRecipientsAsync(action, cancellationToken);

        if (recipients.ActiveUserIds.Count == 0)
        {
            throw new InvalidOperationException("Notification action did not resolve any active recipients.");
        }

        if (ShouldSkipNotificationInsertion(recipients.ActiveUserIds.Count, recipients.EnabledUserIds.Count))
        {
            return Result(
                action,
                ("notificationCount", 0),
                ("recipientUserIds", Array.Empty<Guid>()),
                ("skippedPreferenceCount", recipients.DisabledInAppUserIds.Count));
        }

        foreach (var userId in recipients.EnabledUserIds)
        {
            dbContext.Notifications.Add(new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = action.Title ?? string.Empty,
                Body = action.Body ?? string.Empty,
                SourceType = "Record",
                SourceId = context.RecordId,
                TriggerId = triggerId,
                TriggerLogId = triggerLogId,
                ActionId = action.Id,
                MetadataJson = BuildNotificationMetadata(triggerId, triggerLogId, action.Id, context)
            });
        }

        return Result(
            action,
            ("notificationCount", recipients.EnabledUserIds.Count),
            ("recipientUserIds", recipients.EnabledUserIds),
            ("skippedPreferenceCount", recipients.DisabledInAppUserIds.Count));
    }

    private async Task<NotificationRecipientResolution> ResolveNotificationRecipientsAsync(
        TriggerActionDefinition action,
        CancellationToken cancellationToken)
    {
        var requestedUserIds = (action.RecipientUserIds ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();
        var requestedGroupIds = (action.RecipientGroupIds ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        var directUserIds = requestedUserIds.Length == 0
            ? Array.Empty<Guid>()
            : await dbContext.Users
                .AsNoTracking()
                .Where(user => requestedUserIds.Contains(user.Id) && user.IsActive)
                .Select(user => user.Id)
                .ToArrayAsync(cancellationToken);
        var groupUserIds = requestedGroupIds.Length == 0
            ? Array.Empty<Guid>()
            : await dbContext.UserGroups
                .AsNoTracking()
                .Where(userGroup =>
                    requestedGroupIds.Contains(userGroup.GroupId)
                    && userGroup.User != null
                    && userGroup.User.IsActive
                    && userGroup.Group != null
                    && userGroup.Group.IsActive)
                .Select(userGroup => userGroup.UserId)
                .ToArrayAsync(cancellationToken);

        var activeRecipientIds = directUserIds
            .Concat(groupUserIds)
            .Distinct()
            .ToArray();

        if (activeRecipientIds.Length == 0)
        {
            return new NotificationRecipientResolution(activeRecipientIds, activeRecipientIds, Array.Empty<Guid>());
        }

        var disabledInAppUserIds = await dbContext.NotificationPreferences
            .AsNoTracking()
            .Where(preference => activeRecipientIds.Contains(preference.UserId) && !preference.InAppEnabled)
            .Select(preference => preference.UserId)
            .ToArrayAsync(cancellationToken);

        return new NotificationRecipientResolution(
            activeRecipientIds,
            ExcludeDisabledNotificationRecipients(activeRecipientIds, disabledInAppUserIds),
            disabledInAppUserIds);
    }

    private static IReadOnlyList<Guid> ExcludeDisabledNotificationRecipients(
        IReadOnlyCollection<Guid> activeRecipientIds,
        IReadOnlyCollection<Guid> disabledInAppUserIds)
    {
        return activeRecipientIds
            .Except(disabledInAppUserIds)
            .ToArray();
    }

    private static bool ShouldSkipNotificationInsertion(int activeRecipientCount, int enabledRecipientCount)
    {
        return activeRecipientCount > 0 && enabledRecipientCount == 0;
    }

    private async Task<FormRecord> LoadRecordAsync(Guid recordId, CancellationToken cancellationToken)
    {
        return await dbContext.Records
            .FirstOrDefaultAsync(record => record.Id == recordId && !record.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Trigger action record was not found.");
    }

    private async Task<FormRecord> LoadRecordWithVersionAsync(Guid recordId, CancellationToken cancellationToken)
    {
        return await dbContext.Records
            .Include(record => record.FormVersion)
            .FirstOrDefaultAsync(record => record.Id == recordId && !record.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Trigger action record was not found.");
    }

    private static Dictionary<string, object?> DeserializeValues(JsonDocument valuesJson)
    {
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(valuesJson.RootElement.GetRawText(), JsonOptions)
            ?? new Dictionary<string, object?>();
    }

    private void AddRecordAudit(
        Guid recordId,
        string action,
        Guid? userId,
        Guid triggerId,
        Guid? triggerLogId,
        string actionId,
        object? before,
        object? after)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Record",
            EntityId = recordId,
            Action = action,
            UserId = userId,
            BeforeJson = SerializeNullable(before),
            AfterJson = SerializeNullable(after),
            MetadataJson = BuildMetadata(triggerId, triggerLogId, actionId, null)
        });
    }

    private static IReadOnlyDictionary<string, object?> Result(
        TriggerActionDefinition action,
        params (string Key, object? Value)[] values)
    {
        var result = new Dictionary<string, object?>
        {
            ["actionId"] = action.Id,
            ["type"] = action.Type
        };

        foreach (var (key, value) in values)
        {
            result[key] = value;
        }

        return result;
    }

    private static JsonDocument BuildMetadata(Guid triggerId, Guid? triggerLogId, string actionId, string? message)
    {
        return JsonSerializer.SerializeToDocument(new
        {
            triggerId,
            triggerLogId,
            actionId,
            message
        }, JsonOptions);
    }

    private static JsonDocument BuildNotificationMetadata(
        Guid triggerId,
        Guid? triggerLogId,
        string actionId,
        TriggerEventContext context)
    {
        return JsonSerializer.SerializeToDocument(new
        {
            triggerId,
            triggerLogId,
            actionId,
            formId = context.FormId,
            recordId = context.RecordId,
            eventName = context.EventName
        }, JsonOptions);
    }

    private static JsonDocument? SerializeNullable(object? value)
    {
        return value is null ? null : JsonSerializer.SerializeToDocument(value, JsonOptions);
    }

    private static Guid? NormalizeNullableId(Guid? value)
    {
        return value is null || value == Guid.Empty ? null : value;
    }

    private sealed record NotificationRecipientResolution(
        IReadOnlyList<Guid> ActiveUserIds,
        IReadOnlyList<Guid> EnabledUserIds,
        IReadOnlyList<Guid> DisabledInAppUserIds);
}
