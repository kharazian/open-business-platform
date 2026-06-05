using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Triggers;

public static class TriggerEvents
{
    public const string RecordCreated = "record.created";
    public const string RecordUpdated = "record.updated";
    public const string FieldChanged = "field.changed";
    public const string StatusChanged = "status.changed";
    public const string RecordAssigned = "record.assigned";
    public const string ScheduleOnce = "schedule.once";
    public const string ScheduleDaily = "schedule.daily";
    public const string ScheduleWeekly = "schedule.weekly";
    public const string ScheduleMonthly = "schedule.monthly";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        RecordCreated,
        RecordUpdated,
        FieldChanged,
        StatusChanged,
        RecordAssigned,
        ScheduleOnce,
        ScheduleDaily,
        ScheduleWeekly,
        ScheduleMonthly
    };

    public static IReadOnlySet<string> Scheduled { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        ScheduleOnce,
        ScheduleDaily,
        ScheduleWeekly,
        ScheduleMonthly
    };

    public static bool IsScheduled(string? eventName) => Scheduled.Contains(eventName?.Trim() ?? string.Empty);
}

public static class TriggerConditionModes
{
    public const string All = "all";
}

public static class TriggerConditionTypes
{
    public const string FieldEquals = "field_equals";
    public const string FieldChanged = "field_changed";
    public const string StatusChangedTo = "status_changed_to";
    public const string DepartmentEquals = "department_equals";
    public const string AssignedToUser = "assigned_to_user";
    public const string AssignedToGroup = "assigned_to_group";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        FieldEquals,
        FieldChanged,
        StatusChangedTo,
        DepartmentEquals,
        AssignedToUser,
        AssignedToGroup
    };
}

public static class TriggerActionTypes
{
    public const string WriteAuditEntry = "write_audit_entry";
    public const string SendEmail = "send_email";
    public const string ChangeStatus = "change_status";
    public const string AssignRecord = "assign_record";
    public const string UpdateField = "update_field";
    public const string SendNotification = "send_notification";
    public const string CreateRecord = "create_record";
    public const string CallWebhook = "call_webhook";
    public const string StartWorkflow = "start_workflow";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        WriteAuditEntry,
        SendEmail,
        ChangeStatus,
        AssignRecord,
        UpdateField,
        SendNotification,
        CreateRecord,
        CallWebhook,
        StartWorkflow
    };

    public static IReadOnlySet<string> ScheduledSupported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        SendEmail,
        CallWebhook
    };
}

public static class TriggerScheduleKinds
{
    public const string Once = "once";
    public const string Daily = "daily";
    public const string Weekly = "weekly";
    public const string Monthly = "monthly";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Once,
        Daily,
        Weekly,
        Monthly
    };

    public static string FromEventName(string eventName)
    {
        return eventName switch
        {
            TriggerEvents.ScheduleOnce => Once,
            TriggerEvents.ScheduleDaily => Daily,
            TriggerEvents.ScheduleWeekly => Weekly,
            TriggerEvents.ScheduleMonthly => Monthly,
            _ => string.Empty
        };
    }
}

public static class TriggerExecutionStatuses
{
    public const string Success = "success";
    public const string Failed = "failed";
    public const string Skipped = "skipped";
}

public static class TriggerWorkflowStartResultStatuses
{
    public const string Started = "started";
    public const string Skipped = "skipped";
    public const string Failed = "failed";
}

public static class TriggerWorkflowStartSkipReasons
{
    public const string RecordAlreadyHasActiveWorkflow = "record_already_has_active_workflow";
}

public sealed record TriggerRetryMetadata(Guid SourceLogId);

public sealed record TriggerRetryPolicyDefinition(
    bool IsEnabled = true,
    int MaxAttempts = 3,
    int DelaySeconds = 60);

public sealed record TriggerScheduleDefinition(
    string Kind,
    string TimeZone,
    DateTimeOffset StartAt);

public sealed record TriggerConditionDefinition(
    string Type,
    string? FieldId = null,
    object? Value = null,
    string? Status = null,
    Guid? DepartmentId = null,
    Guid? UserId = null,
    Guid? GroupId = null);

public sealed record TriggerConditionGroupDefinition(
    string Mode,
    IReadOnlyList<TriggerConditionDefinition> Conditions);

public sealed record TriggerActionDefinition(
    string Id,
    string Type,
    string? Message = null,
    IReadOnlyList<string>? To = null,
    string? Subject = null,
    string? Body = null,
    string? Status = null,
    Guid? AssignedToUserId = null,
    Guid? AssignedGroupId = null,
    string? FieldId = null,
    object? Value = null,
    string? Title = null,
    IReadOnlyList<Guid>? RecipientUserIds = null,
    IReadOnlyList<Guid>? RecipientGroupIds = null,
    Guid? TargetFormId = null,
    IReadOnlyDictionary<string, TriggerActionValueDefinition>? Values = null,
    string? WebhookUrl = null,
    string? WebhookMethod = null,
    IReadOnlyDictionary<string, string>? WebhookHeaders = null,
    object? WebhookBody = null,
    Guid? WorkflowDefinitionId = null);

public sealed record TriggerActionValueDefinition(
    object? Literal = null,
    string? SourceFieldId = null);

public sealed record TriggerTargetFormSchema(
    Guid FormId,
    Guid FormVersionId,
    FormSchemaDefinition Schema);

public sealed record TriggerWorkflowStartTarget(
    Guid WorkflowDefinitionId,
    Guid FormId,
    bool IsEnabled,
    string Status,
    Guid? CurrentVersionId);

public sealed record CreateTriggerRequest(
    string Name,
    string? Description,
    string EventName,
    TriggerConditionGroupDefinition? Conditions,
    IReadOnlyList<TriggerActionDefinition> Actions,
    bool IsEnabled = true,
    TriggerRetryPolicyDefinition? RetryPolicy = null,
    TriggerScheduleDefinition? Schedule = null);

public sealed record UpdateTriggerRequest(
    string Name,
    string? Description,
    string EventName,
    TriggerConditionGroupDefinition? Conditions,
    IReadOnlyList<TriggerActionDefinition> Actions,
    bool IsEnabled,
    string ConcurrencyStamp,
    TriggerRetryPolicyDefinition? RetryPolicy = null,
    TriggerScheduleDefinition? Schedule = null);

public sealed record TriggerSummaryDto(
    Guid Id,
    Guid FormId,
    string Name,
    string? Description,
    string EventName,
    bool IsEnabled,
    TriggerRetryPolicyDefinition RetryPolicy,
    TriggerScheduleDefinition? Schedule,
    DateTimeOffset? ScheduleNextRunAt,
    DateTimeOffset? ScheduleLastRunAt,
    int ConditionCount,
    int ActionCount,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById);

public sealed record TriggerDetailDto(
    Guid Id,
    Guid FormId,
    string Name,
    string? Description,
    string EventName,
    TriggerConditionGroupDefinition Conditions,
    IReadOnlyList<TriggerActionDefinition> Actions,
    bool IsEnabled,
    TriggerRetryPolicyDefinition RetryPolicy,
    TriggerScheduleDefinition? Schedule,
    DateTimeOffset? ScheduleNextRunAt,
    DateTimeOffset? ScheduleLastRunAt,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById);

public sealed record TriggerExecutionLogDto(
    Guid Id,
    Guid TriggerId,
    Guid FormId,
    string EventName,
    string EntityType,
    Guid EntityId,
    string Status,
    object? Input,
    object? Result,
    string? ErrorMessage,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt,
    Guid? RetryOfLogId = null,
    string? RetryState = null,
    int AutoRetryAttemptCount = 0,
    int AutoRetryMaxAttempts = 0,
    DateTimeOffset? AutoRetryNextAttemptAt = null);

public sealed record TriggerValidationError(string Path, string Code, string Message);

public sealed record TriggerValidationResult(IReadOnlyList<TriggerValidationError> Errors)
{
    public bool Valid => Errors.Count == 0;
}

public sealed record TriggerErrorResponse(string Message, IReadOnlyList<TriggerValidationError>? Errors = null);

public sealed class TriggerManagementException : Exception
{
    public TriggerManagementException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = Array.Empty<TriggerValidationError>();
    }

    public TriggerManagementException(int statusCode, string message, IReadOnlyList<TriggerValidationError> errors)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public int StatusCode { get; }

    public IReadOnlyList<TriggerValidationError> Errors { get; }
}
