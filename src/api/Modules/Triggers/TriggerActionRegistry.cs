using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpenBusinessPlatform.Api.Domain.Entities;
using OpenBusinessPlatform.Api.Infrastructure.Persistence;
using OpenBusinessPlatform.Api.Modules.Forms;
using OpenBusinessPlatform.Api.Modules.Notifications;
using OpenBusinessPlatform.Api.Modules.Workflows;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed class TriggerActionRegistry
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly OpenBusinessPlatformDbContext dbContext;
    private readonly IEmailSender emailSender;
    private readonly IHttpClientFactory httpClientFactory;

    public TriggerActionRegistry(
        OpenBusinessPlatformDbContext dbContext,
        IEmailSender emailSender,
        IHttpClientFactory httpClientFactory)
    {
        this.dbContext = dbContext;
        this.emailSender = emailSender;
        this.httpClientFactory = httpClientFactory;
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
            TriggerActionTypes.CreateRecord => await ExecuteCreateRecordAsync(action, context, triggerId, triggerLogId, cancellationToken),
            TriggerActionTypes.CallWebhook => await ExecuteCallWebhookAsync(action, context, triggerId, triggerLogId, cancellationToken),
            TriggerActionTypes.StartWorkflow => await ExecuteStartWorkflowAsync(action, context, triggerId, triggerLogId, cancellationToken),
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

    private async Task<IReadOnlyDictionary<string, object?>> ExecuteCreateRecordAsync(
        TriggerActionDefinition action,
        TriggerEventContext context,
        Guid triggerId,
        Guid? triggerLogId,
        CancellationToken cancellationToken)
    {
        if (action.TargetFormId is null || action.TargetFormId == Guid.Empty)
        {
            throw new InvalidOperationException("Create record action target form is required.");
        }

        if (action.Values is null || action.Values.Count == 0)
        {
            throw new InvalidOperationException("Create record action values are required.");
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
            ?? throw new InvalidOperationException("Create record target form is not published or was not found.");

        var targetVersion = targetForm.CurrentVersion
            ?? throw new InvalidOperationException("Create record target form version was not found.");
        var schema = targetVersion.SchemaJson.RootElement.Deserialize<FormSchemaDefinition>(JsonOptions)
            ?? throw new InvalidOperationException("Create record target form schema was not found.");
        var values = ResolveCreateRecordValues(action, context);
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
            ExtraPropertiesJson = BuildCreatedRecordMetadata(triggerId, triggerLogId, action.Id, context)
        };

        dbContext.Records.Add(record);
        AddCreatedRecordAudit(record.Id, context.ActorUserId, triggerId, triggerLogId, action.Id, context.RecordId);

        return Result(
            action,
            ("targetFormId", targetForm.Id),
            ("formVersionId", targetVersion.Id),
            ("recordId", record.Id));
    }

    private async Task<IReadOnlyDictionary<string, object?>> ExecuteCallWebhookAsync(
        TriggerActionDefinition action,
        TriggerEventContext context,
        Guid triggerId,
        Guid? triggerLogId,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(action.WebhookUrl, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException("Webhook action URL must be an absolute http or https URL.");
        }

        var method = new HttpMethod(string.IsNullOrWhiteSpace(action.WebhookMethod) ? "POST" : action.WebhookMethod.Trim().ToUpperInvariant());
        using var request = new HttpRequestMessage(method, uri);

        foreach (var header in action.WebhookHeaders ?? new Dictionary<string, string>())
        {
            request.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (method.Method is not ("GET" or "DELETE"))
        {
            var payload = action.WebhookBody ?? BuildWebhookPayload(triggerId, triggerLogId, action.Id, context);
            request.Content = new StringContent(JsonSerializer.Serialize(payload, JsonOptions), Encoding.UTF8, "application/json");
        }

        var client = httpClientFactory.CreateClient("trigger-webhooks");
        using var response = await client.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Webhook action returned HTTP {(int)response.StatusCode}.");
        }

        return Result(
            action,
            ("url", uri.ToString()),
            ("method", method.Method),
            ("statusCode", (int)response.StatusCode),
            ("responseBody", Truncate(responseBody, 2000)));
    }

    private async Task<IReadOnlyDictionary<string, object?>> ExecuteStartWorkflowAsync(
        TriggerActionDefinition action,
        TriggerEventContext context,
        Guid triggerId,
        Guid? triggerLogId,
        CancellationToken cancellationToken)
    {
        if (action.WorkflowDefinitionId is null || action.WorkflowDefinitionId == Guid.Empty)
        {
            throw new InvalidOperationException("Start workflow action workflow definition id is required.");
        }

        if (context.RecordId == Guid.Empty || TriggerEvents.IsScheduled(context.EventName))
        {
            throw new InvalidOperationException("Start workflow actions require a current record context.");
        }

        var record = await LoadRecordAsync(context.RecordId, cancellationToken);

        if (record.FormId != context.FormId)
        {
            throw new InvalidOperationException("Start workflow action record form does not match the trigger context.");
        }

        if (record.WorkflowDefinitionId is not null
            || record.WorkflowDefinitionVersionId is not null
            || !string.IsNullOrWhiteSpace(record.WorkflowStateKey))
        {
            return Result(
                action,
                ("workflowStartStatus", TriggerWorkflowStartResultStatuses.Skipped),
                ("reason", TriggerWorkflowStartSkipReasons.RecordAlreadyHasActiveWorkflow),
                ("recordId", record.Id),
                ("workflowDefinitionId", record.WorkflowDefinitionId),
                ("workflowDefinitionVersionId", record.WorkflowDefinitionVersionId),
                ("stateKey", record.WorkflowStateKey));
        }

        var workflow = await dbContext.Workflows
            .Include(candidate => candidate.CurrentVersion)
            .FirstOrDefaultAsync(candidate => candidate.Id == action.WorkflowDefinitionId.Value && !candidate.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Workflow target was not found.");

        if (workflow.FormId != record.FormId)
        {
            throw new InvalidOperationException("Workflow target belongs to a different form.");
        }

        if (!workflow.IsEnabled)
        {
            throw new InvalidOperationException("Workflow target is disabled.");
        }

        if (!string.Equals(workflow.Status, WorkflowDefinitionStatuses.Published, StringComparison.Ordinal)
            || workflow.CurrentVersionId is null
            || workflow.CurrentVersion is null)
        {
            throw new InvalidOperationException("Workflow target has no current published version.");
        }

        var version = workflow.CurrentVersion;
        var config = version.ConfigJson.RootElement.Deserialize<WorkflowDefinitionConfig>(JsonOptions)
            ?? throw new InvalidOperationException("Workflow target config was not found.");
        config = WorkflowDefinitionValidator.NormalizeConfig(config);
        var initialState = config.States.FirstOrDefault(state => string.Equals(state.Key, config.InitialStateKey, StringComparison.Ordinal));

        if (initialState is null)
        {
            throw new InvalidOperationException("Workflow target initial state was not found.");
        }

        var previousStatus = record.Status;
        record.WorkflowDefinitionId = workflow.Id;
        record.WorkflowDefinitionVersionId = version.Id;
        record.WorkflowStateKey = config.InitialStateKey;
        record.Status = config.InitialStateKey;
        record.UpdatedById = context.ActorUserId;

        AddWorkflowStartedHistory(record, workflow, version, config.InitialStateKey, context, triggerId, triggerLogId, action.Id);
        AddWorkflowStartedAudit(record, previousStatus, context, triggerId, triggerLogId, action.Id);

        return Result(
            action,
            ("workflowStartStatus", TriggerWorkflowStartResultStatuses.Started),
            ("recordId", record.Id),
            ("workflowDefinitionId", workflow.Id),
            ("workflowDefinitionVersionId", version.Id),
            ("previousStatus", previousStatus),
            ("stateKey", config.InitialStateKey),
            ("dispatchStatusChanged", false));
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

    public static IReadOnlyDictionary<string, object?> ResolveCreateRecordValues(
        TriggerActionDefinition action,
        TriggerEventContext context)
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var (fieldId, valueDefinition) in action.Values ?? new Dictionary<string, TriggerActionValueDefinition>())
        {
            if (!string.IsNullOrWhiteSpace(valueDefinition.SourceFieldId))
            {
                if (!context.After.Values.TryGetValue(valueDefinition.SourceFieldId, out var sourceValue))
                {
                    throw new InvalidOperationException($"Create record source field '{valueDefinition.SourceFieldId}' was not found on the source record.");
                }

                values[fieldId] = sourceValue;
                continue;
            }

            values[fieldId] = valueDefinition.Literal;
        }

        return values;
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

    private void AddCreatedRecordAudit(
        Guid recordId,
        Guid? userId,
        Guid triggerId,
        Guid? triggerLogId,
        string actionId,
        Guid sourceRecordId)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Record",
            EntityId = recordId,
            Action = "record_created_by_trigger",
            UserId = userId,
            MetadataJson = JsonSerializer.SerializeToDocument(new
            {
                triggerId,
                triggerLogId,
                actionId,
                sourceRecordId
            }, JsonOptions)
        });
    }

    private void AddWorkflowStartedHistory(
        FormRecord record,
        WorkflowDefinition workflow,
        WorkflowDefinitionVersion version,
        string initialStateKey,
        TriggerEventContext context,
        Guid triggerId,
        Guid? triggerLogId,
        string actionId)
    {
        dbContext.WorkflowHistory.Add(new WorkflowHistoryEntry
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = workflow.Id,
            WorkflowDefinitionVersionId = version.Id,
            FormId = record.FormId,
            RecordId = record.Id,
            FromStateKey = null,
            ToStateKey = initialStateKey,
            TransitionKey = null,
            Action = RecordWorkflowHistoryActions.Started,
            ActorUserId = context.ActorUserId,
            CreatedById = context.ActorUserId,
            MetadataJson = JsonSerializer.SerializeToDocument(new
            {
                workflow.Name,
                version.VersionNumber,
                startedByTrigger = true,
                triggerId,
                triggerLogId,
                actionId,
                context.EventName
            }, JsonOptions)
        });
    }

    private void AddWorkflowStartedAudit(
        FormRecord record,
        string previousStatus,
        TriggerEventContext context,
        Guid triggerId,
        Guid? triggerLogId,
        string actionId)
    {
        dbContext.AuditLogs.Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = "Record",
            EntityId = record.Id,
            Action = "record_workflow_started_by_trigger",
            UserId = context.ActorUserId,
            BeforeJson = SerializeNullable(new
            {
                Status = previousStatus
            }),
            AfterJson = SerializeNullable(new
            {
                Status = record.Status,
                WorkflowDefinitionId = record.WorkflowDefinitionId,
                WorkflowDefinitionVersionId = record.WorkflowDefinitionVersionId,
                WorkflowStateKey = record.WorkflowStateKey
            }),
            MetadataJson = JsonSerializer.SerializeToDocument(new
            {
                triggerId,
                triggerLogId,
                actionId,
                context.EventName,
                WorkflowDefinitionId = record.WorkflowDefinitionId,
                WorkflowDefinitionVersionId = record.WorkflowDefinitionVersionId,
                StateKey = record.WorkflowStateKey,
                dispatchStatusChanged = false
            }, JsonOptions)
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

    private static object BuildWebhookPayload(
        Guid triggerId,
        Guid? triggerLogId,
        string actionId,
        TriggerEventContext context)
    {
        return new
        {
            triggerId,
            triggerLogId,
            actionId,
            context.EventName,
            context.FormId,
            context.RecordId,
            context.ActorUserId,
            context.ChangedFieldIds,
            context.PreviousStatus,
            context.CurrentStatus,
            context.OccurredAt,
            before = context.Before,
            after = context.After
        };
    }

    private static JsonDocument BuildCreatedRecordMetadata(
        Guid triggerId,
        Guid? triggerLogId,
        string actionId,
        TriggerEventContext context)
    {
        return JsonSerializer.SerializeToDocument(new
        {
            createdByTrigger = true,
            triggerId,
            triggerLogId,
            actionId,
            sourceFormId = context.FormId,
            sourceRecordId = context.RecordId,
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

    private static string Truncate(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    private sealed record NotificationRecipientResolution(
        IReadOnlyList<Guid> ActiveUserIds,
        IReadOnlyList<Guid> EnabledUserIds,
        IReadOnlyList<Guid> DisabledInAppUserIds);
}
