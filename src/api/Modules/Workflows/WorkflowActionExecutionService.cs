using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Notifications;
using OpenBusinessPlatform.Api.Modules.Triggers;

namespace OpenBusinessPlatform.Api.Modules.Workflows;

public sealed class WorkflowActionExecutionService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly IEmailSender emailSender;

    public WorkflowActionExecutionService(OpenBusinessPlatformDbContext dbContext, IEmailSender emailSender)
    {
        this.dbContext = dbContext;
        this.emailSender = emailSender;
    }

    public async Task<IReadOnlyList<WorkflowActionExecutionResult>> ExecuteTransitionActionsAsync(
        FormRecord record,
        WorkflowTransitionDefinition transition,
        WorkflowTransitionActionContext context,
        CancellationToken cancellationToken)
    {
        var results = new List<WorkflowActionExecutionResult>();

        foreach (var action in transition.Actions ?? Array.Empty<WorkflowActionDefinition>())
        {
            var startedAt = DateTimeOffset.UtcNow;

            try
            {
                var result = await ExecuteActionAsync(record, action, context, cancellationToken);
                var completedAt = DateTimeOffset.UtcNow;
                var actionResult = new WorkflowActionExecutionResult(
                    action.Id,
                    action.Type,
                    WorkflowActionExecutionStatuses.Succeeded,
                    null,
                    startedAt,
                    completedAt,
                    result);
                results.Add(actionResult);
                AddActionHistory(context, actionResult);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                var completedAt = DateTimeOffset.UtcNow;
                var actionResult = new WorkflowActionExecutionResult(
                    action.Id,
                    action.Type,
                    WorkflowActionExecutionStatuses.Failed,
                    exception.Message,
                    startedAt,
                    completedAt,
                    new Dictionary<string, object?>());
                results.Add(actionResult);

                if (IsBlockingAction(action))
                {
                    throw new WorkflowActionExecutionException(action, results, exception);
                }

                AddActionHistory(context, actionResult);
            }
        }

        return results;
    }

    public async Task PersistRolledBackActionFailureAsync(
        WorkflowTransitionActionContext context,
        IReadOnlyList<WorkflowActionExecutionResult> results,
        CancellationToken cancellationToken)
    {
        var rolledBackResults = results
            .Select(result => string.Equals(result.Status, WorkflowActionExecutionStatuses.Succeeded, StringComparison.Ordinal)
                    && IsDatabaseBackedActionType(result.ActionType)
                ? result with { Status = WorkflowActionExecutionStatuses.RolledBack }
                : result)
            .ToArray();

        foreach (var result in rolledBackResults)
        {
            AddActionHistory(context, result);
        }

        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Record",
            EntityId = context.RecordId,
            Action = "record_workflow_action_failed",
            UserId = context.ActorUserId,
            MetadataJson = Serialize(new
            {
                context.WorkflowDefinitionId,
                context.WorkflowDefinitionVersionId,
                context.TransitionKey,
                Actions = rolledBackResults.Select(ToMetadata).ToArray()
            })
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public static TriggerActionDefinition ToTriggerActionDefinition(WorkflowActionDefinition action)
    {
        return new TriggerActionDefinition(
            action.Id,
            action.Type,
            action.Message,
            action.To,
            action.Subject,
            action.Body,
            action.Status,
            action.AssignedToUserId,
            action.AssignedGroupId,
            action.FieldId,
            action.Value,
            action.Title,
            action.RecipientUserIds,
            action.RecipientGroupIds,
            action.TargetFormId,
            action.Values?.ToDictionary(
                pair => pair.Key,
                pair => new TriggerActionValueDefinition(pair.Value.Literal, pair.Value.SourceFieldId),
                StringComparer.Ordinal));
    }

    private async Task<IReadOnlyDictionary<string, object?>> ExecuteActionAsync(
        FormRecord record,
        WorkflowActionDefinition action,
        WorkflowTransitionActionContext context,
        CancellationToken cancellationToken)
    {
        return action.Type switch
        {
            WorkflowActionTypes.WriteAuditEntry => ExecuteWriteAuditEntry(record, action, context),
            WorkflowActionTypes.SendEmail => await ExecuteSendEmailAsync(action, cancellationToken),
            WorkflowActionTypes.AssignRecord => await ExecuteAssignRecordAsync(record, action, context, cancellationToken),
            WorkflowActionTypes.UpdateField => await ExecuteUpdateFieldAsync(record, action, context, cancellationToken),
            WorkflowActionTypes.SendNotification => await ExecuteSendNotificationAsync(record, action, context, cancellationToken),
            WorkflowActionTypes.CreateRecord => await ExecuteCreateRecordAsync(record, action, context, cancellationToken),
            _ => throw new InvalidOperationException($"Workflow action type '{action.Type}' is not supported.")
        };
    }

    private IReadOnlyDictionary<string, object?> ExecuteWriteAuditEntry(
        FormRecord record,
        WorkflowActionDefinition action,
        WorkflowTransitionActionContext context)
    {
        if (string.IsNullOrWhiteSpace(action.Message))
        {
            throw new InvalidOperationException("Workflow audit action message is required.");
        }

        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Record",
            EntityId = record.Id,
            Action = "workflow_audit_entry",
            UserId = context.ActorUserId,
            MetadataJson = BuildMetadata(context, action.Id, action.Type, new { action.Message })
        });

        return Result(action, ("message", action.Message));
    }

    private async Task<IReadOnlyDictionary<string, object?>> ExecuteSendEmailAsync(
        WorkflowActionDefinition action,
        CancellationToken cancellationToken)
    {
        var recipients = action.To ?? Array.Empty<string>();

        if (recipients.Count == 0)
        {
            throw new InvalidOperationException("Workflow email action requires at least one recipient.");
        }

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

    private async Task<IReadOnlyDictionary<string, object?>> ExecuteAssignRecordAsync(
        FormRecord record,
        WorkflowActionDefinition action,
        WorkflowTransitionActionContext context,
        CancellationToken cancellationToken)
    {
        var assignedToUserId = NormalizeNullableId(action.AssignedToUserId);
        var assignedGroupId = NormalizeNullableId(action.AssignedGroupId);
        var hasUser = assignedToUserId is not null;
        var hasGroup = assignedGroupId is not null;

        if (hasUser == hasGroup)
        {
            throw new InvalidOperationException("Workflow assign action requires exactly one user or group target.");
        }

        if (assignedToUserId is not null)
        {
            var userExists = await dbContext.Users
                .AsNoTracking()
                .AnyAsync(user => user.Id == assignedToUserId.Value && user.IsActive, cancellationToken);

            if (!userExists)
            {
                throw new InvalidOperationException("Workflow assign action user target is not active or was not found.");
            }
        }

        if (assignedGroupId is not null)
        {
            var groupExists = await dbContext.Groups
                .AsNoTracking()
                .AnyAsync(group => group.Id == assignedGroupId.Value && group.IsActive, cancellationToken);

            if (!groupExists)
            {
                throw new InvalidOperationException("Workflow assign action group target is not active or was not found.");
            }
        }

        var previousAssignedToUserId = record.AssignedToUserId;
        var previousAssignedGroupId = record.AssignedGroupId;
        record.AssignedToUserId = assignedToUserId;
        record.AssignedGroupId = assignedGroupId;
        record.UpdatedById = context.ActorUserId;
        AddRecordAudit(record.Id, "record_assigned_by_workflow_action", context.ActorUserId, context, action.Id, action.Type, previousAssignedToUserId, previousAssignedGroupId);

        return Result(
            action,
            ("assignedToUserId", record.AssignedToUserId),
            ("assignedGroupId", record.AssignedGroupId));
    }

    private async Task<IReadOnlyDictionary<string, object?>> ExecuteUpdateFieldAsync(
        FormRecord record,
        WorkflowActionDefinition action,
        WorkflowTransitionActionContext context,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(action.FieldId))
        {
            throw new InvalidOperationException("Workflow update field action field id is required.");
        }

        await dbContext.Entry(record).Reference(candidate => candidate.FormVersion).LoadAsync(cancellationToken);
        var schema = record.FormVersion?.SchemaJson.RootElement.Deserialize<FormSchemaDefinition>(JsonOptions)
            ?? throw new InvalidOperationException("Workflow action record form schema was not found.");
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
        AddRecordAudit(record.Id, "record_field_updated_by_workflow_action", context.ActorUserId, context, action.Id, action.Type, previousValue, action.Value);

        return Result(action, ("fieldId", action.FieldId), ("previousValue", previousValue), ("value", action.Value));
    }

    private async Task<IReadOnlyDictionary<string, object?>> ExecuteSendNotificationAsync(
        FormRecord record,
        WorkflowActionDefinition action,
        WorkflowTransitionActionContext context,
        CancellationToken cancellationToken)
    {
        var recipients = await ResolveNotificationRecipientsAsync(action, cancellationToken);

        if (recipients.ActiveUserIds.Count == 0)
        {
            throw new InvalidOperationException("Workflow notification action did not resolve any active recipients.");
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
                SourceType = "WorkflowAction",
                SourceId = record.Id,
                ActionId = action.Id,
                MetadataJson = BuildMetadata(context, action.Id, action.Type, new { record.FormId, recordId = record.Id })
            });
        }

        return Result(
            action,
            ("notificationCount", recipients.EnabledUserIds.Count),
            ("recipientUserIds", recipients.EnabledUserIds),
            ("skippedPreferenceCount", recipients.DisabledInAppUserIds.Count));
    }

    private async Task<IReadOnlyDictionary<string, object?>> ExecuteCreateRecordAsync(
        FormRecord sourceRecord,
        WorkflowActionDefinition action,
        WorkflowTransitionActionContext context,
        CancellationToken cancellationToken)
    {
        if (action.TargetFormId is null || action.TargetFormId == Guid.Empty)
        {
            throw new InvalidOperationException("Workflow create record action target form is required.");
        }

        if (action.Values is null || action.Values.Count == 0)
        {
            throw new InvalidOperationException("Workflow create record action values are required.");
        }

        var targetForm = await dbContext.Forms
            .Include(form => form.CurrentVersion)
            .FirstOrDefaultAsync(
                form =>
                    form.Id == action.TargetFormId.Value
                    && !form.IsDeleted
                    && form.Status == FormStatuses.Published
                    && form.CurrentVersionId != null
                    && form.CurrentVersion != null,
                cancellationToken)
            ?? throw new InvalidOperationException("Workflow create record target form is not published or was not found.");
        var targetVersion = targetForm.CurrentVersion
            ?? throw new InvalidOperationException("Workflow create record target form version was not found.");
        var schema = targetVersion.SchemaJson.RootElement.Deserialize<FormSchemaDefinition>(JsonOptions)
            ?? throw new InvalidOperationException("Workflow create record target form schema was not found.");
        var values = ResolveCreateRecordValues(action, DeserializeValues(sourceRecord.ValuesJson));
        var validation = FormSchemaValidator.ValidateRecordValues(schema, values);

        if (!validation.Valid)
        {
            throw new InvalidOperationException(string.Join(" ", validation.Errors.Select(error => error.Message)));
        }

        var record = new FormRecord
        {
            Id = Guid.NewGuid(),
            FormId = targetForm.Id,
            FormVersionId = targetVersion.Id,
            Status = RecordStatuses.Active,
            OwnerId = context.ActorUserId,
            CreatedById = context.ActorUserId,
            ValuesJson = JsonSerializer.SerializeToDocument(values, JsonOptions),
            ExtraPropertiesJson = BuildCreatedRecordMetadata(context, action.Id, action.Type, sourceRecord.Id)
        };

        dbContext.Records.Add(record);
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Record",
            EntityId = record.Id,
            Action = "record_created_by_workflow_action",
            UserId = context.ActorUserId,
            MetadataJson = BuildMetadata(context, action.Id, action.Type, new { SourceRecordId = sourceRecord.Id })
        });

        return Result(
            action,
            ("targetFormId", targetForm.Id),
            ("formVersionId", targetVersion.Id),
            ("recordId", record.Id));
    }

    private async Task<NotificationRecipientResolution> ResolveNotificationRecipientsAsync(
        WorkflowActionDefinition action,
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
            activeRecipientIds.Except(disabledInAppUserIds).ToArray(),
            disabledInAppUserIds);
    }

    private void AddActionHistory(WorkflowTransitionActionContext context, WorkflowActionExecutionResult result)
    {
        dbContext.WorkflowHistory.Add(new WorkflowHistoryEntry
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = context.WorkflowDefinitionId,
            WorkflowDefinitionVersionId = context.WorkflowDefinitionVersionId,
            FormId = context.FormId,
            RecordId = context.RecordId,
            FromStateKey = context.FromStateKey,
            ToStateKey = context.ToStateKey,
            TransitionKey = context.TransitionKey,
            Action = string.Equals(result.Status, WorkflowActionExecutionStatuses.Succeeded, StringComparison.Ordinal)
                ? RecordWorkflowHistoryActions.ActionSucceeded
                : RecordWorkflowHistoryActions.ActionFailed,
            ActorUserId = context.ActorUserId,
            CreatedById = context.ActorUserId,
            MetadataJson = Serialize(new
            {
                context.TransitionName,
                Action = ToMetadata(result)
            })
        });
    }

    private void AddRecordAudit(
        Guid recordId,
        string action,
        Guid? userId,
        WorkflowTransitionActionContext context,
        string actionId,
        string actionType,
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
            MetadataJson = BuildMetadata(context, actionId, actionType, null)
        });
    }

    private static bool IsBlockingAction(WorkflowActionDefinition action)
    {
        return action.Type is
            WorkflowActionTypes.WriteAuditEntry or
            WorkflowActionTypes.AssignRecord or
            WorkflowActionTypes.UpdateField or
            WorkflowActionTypes.CreateRecord;
    }

    private static bool IsDatabaseBackedActionType(string actionType)
    {
        return actionType is
            WorkflowActionTypes.WriteAuditEntry or
            WorkflowActionTypes.AssignRecord or
            WorkflowActionTypes.UpdateField or
            WorkflowActionTypes.SendNotification or
            WorkflowActionTypes.CreateRecord;
    }

    private static Dictionary<string, object?> DeserializeValues(JsonDocument valuesJson)
    {
        return JsonSerializer.Deserialize<Dictionary<string, object?>>(valuesJson.RootElement.GetRawText(), JsonOptions)
            ?? new Dictionary<string, object?>();
    }

    private static Dictionary<string, object?> ResolveCreateRecordValues(
        WorkflowActionDefinition action,
        IReadOnlyDictionary<string, object?> sourceValues)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var (fieldId, valueDefinition) in action.Values ?? new Dictionary<string, WorkflowActionValueDefinition>())
        {
            if (!string.IsNullOrWhiteSpace(valueDefinition.SourceFieldId))
            {
                if (!sourceValues.TryGetValue(valueDefinition.SourceFieldId, out var sourceValue))
                {
                    throw new InvalidOperationException($"Workflow create record source field '{valueDefinition.SourceFieldId}' was not found on the source record.");
                }

                values[fieldId] = sourceValue;
                continue;
            }

            values[fieldId] = valueDefinition.Literal;
        }

        return values;
    }

    private static IReadOnlyDictionary<string, object?> Result(
        WorkflowActionDefinition action,
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

    private static object ToMetadata(WorkflowActionExecutionResult result)
    {
        return new
        {
            actionId = result.ActionId,
            actionType = result.ActionType,
            status = result.Status,
            errorMessage = result.ErrorMessage,
            startedAt = result.StartedAt,
            completedAt = result.CompletedAt,
            result = result.Result
        };
    }

    private static JsonDocument BuildMetadata(
        WorkflowTransitionActionContext context,
        string actionId,
        string actionType,
        object? additional)
    {
        return Serialize(new
        {
            context.WorkflowDefinitionId,
            context.WorkflowDefinitionVersionId,
            context.TransitionKey,
            actionId,
            actionType,
            additional
        });
    }

    private static JsonDocument BuildCreatedRecordMetadata(
        WorkflowTransitionActionContext context,
        string actionId,
        string actionType,
        Guid sourceRecordId)
    {
        return Serialize(new
        {
            createdByWorkflowAction = true,
            context.WorkflowDefinitionId,
            context.WorkflowDefinitionVersionId,
            context.TransitionKey,
            actionId,
            actionType,
            sourceFormId = context.FormId,
            sourceRecordId
        });
    }

    private static JsonDocument Serialize<TValue>(TValue value)
    {
        return JsonSerializer.SerializeToDocument(value, JsonOptions);
    }

    private static JsonDocument? SerializeNullable(object? value)
    {
        return value is null ? null : JsonSerializer.SerializeToDocument(value, JsonOptions);
    }

    private static Guid? NormalizeNullableId(Guid? value)
    {
        return value is null || value == Guid.Empty ? null : value;
    }

    private static bool ShouldSkipNotificationInsertion(int activeRecipientCount, int enabledRecipientCount)
    {
        return activeRecipientCount > 0 && enabledRecipientCount == 0;
    }

    private sealed record NotificationRecipientResolution(
        IReadOnlyList<Guid> ActiveUserIds,
        IReadOnlyList<Guid> EnabledUserIds,
        IReadOnlyList<Guid> DisabledInAppUserIds);
}

public sealed record WorkflowTransitionActionContext(
    Guid WorkflowDefinitionId,
    Guid WorkflowDefinitionVersionId,
    Guid FormId,
    Guid RecordId,
    string? FromStateKey,
    string ToStateKey,
    string TransitionKey,
    string TransitionName,
    Guid? ActorUserId);

public sealed record WorkflowActionExecutionResult(
    string ActionId,
    string ActionType,
    string Status,
    string? ErrorMessage,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt,
    IReadOnlyDictionary<string, object?> Result);

public sealed class WorkflowActionExecutionException : Exception
{
    public WorkflowActionExecutionException(
        WorkflowActionDefinition failedAction,
        IReadOnlyList<WorkflowActionExecutionResult> results,
        Exception innerException)
        : base($"Workflow action '{failedAction.Id}' ({failedAction.Type}) failed: {innerException.Message}", innerException)
    {
        FailedAction = failedAction;
        Results = results;
    }

    public WorkflowActionDefinition FailedAction { get; }

    public IReadOnlyList<WorkflowActionExecutionResult> Results { get; }
}

public static class WorkflowActionExecutionStatuses
{
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public const string RolledBack = "rolled_back";
}
