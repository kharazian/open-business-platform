namespace OpenBusinessPlatform.Api.Modules.Triggers;

public static class TriggerEvents
{
    public const string RecordCreated = "record.created";
    public const string RecordUpdated = "record.updated";
    public const string FieldChanged = "field.changed";
    public const string StatusChanged = "status.changed";
    public const string RecordAssigned = "record.assigned";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        RecordCreated,
        RecordUpdated,
        FieldChanged,
        StatusChanged,
        RecordAssigned
    };
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

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        WriteAuditEntry,
        SendEmail,
        ChangeStatus,
        AssignRecord,
        UpdateField
    };
}

public static class TriggerExecutionStatuses
{
    public const string Success = "success";
    public const string Failed = "failed";
    public const string Skipped = "skipped";
}

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
    object? Value = null);

public sealed record CreateTriggerRequest(
    string Name,
    string? Description,
    string EventName,
    TriggerConditionGroupDefinition? Conditions,
    IReadOnlyList<TriggerActionDefinition> Actions,
    bool IsEnabled = true);

public sealed record UpdateTriggerRequest(
    string Name,
    string? Description,
    string EventName,
    TriggerConditionGroupDefinition? Conditions,
    IReadOnlyList<TriggerActionDefinition> Actions,
    bool IsEnabled,
    string ConcurrencyStamp);

public sealed record TriggerSummaryDto(
    Guid Id,
    Guid FormId,
    string Name,
    string? Description,
    string EventName,
    bool IsEnabled,
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
    DateTimeOffset CreatedAt);

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
