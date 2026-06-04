namespace OpenBusinessPlatform.Api.Modules.Workflows;

public static class WorkflowApprovalModes
{
    public const string Any = "any";
    public const string All = "all";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Any,
        All
    };
}

public static class WorkflowAssigneeRuleTypes
{
    public const string User = "user";
    public const string Group = "group";
    public const string DepartmentManager = "department_manager";
    public const string RecordOwner = "record_owner";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        User,
        Group,
        DepartmentManager,
        RecordOwner
    };
}

public static class WorkflowActionTypes
{
    public const string WriteAuditEntry = "write_audit_entry";
    public const string SendEmail = "send_email";
    public const string ChangeStatus = "change_status";
    public const string AssignRecord = "assign_record";
    public const string UpdateField = "update_field";
    public const string SendNotification = "send_notification";
    public const string CreateRecord = "create_record";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        WriteAuditEntry,
        SendEmail,
        ChangeStatus,
        AssignRecord,
        UpdateField,
        SendNotification,
        CreateRecord
    };
}

public sealed record WorkflowStateDefinition(
    string Key,
    string Name,
    bool IsFinal = false);

public sealed record WorkflowTransitionDefinition(
    string Key,
    string Name,
    string FromStateKey,
    string ToStateKey,
    string? ApprovalStepKey = null,
    IReadOnlyList<WorkflowActionDefinition>? Actions = null);

public sealed record WorkflowApprovalStepDefinition(
    string Key,
    string Name,
    string Mode,
    IReadOnlyList<WorkflowAssigneeRuleDefinition> AssigneeRules);

public sealed record WorkflowAssigneeRuleDefinition(
    string Type,
    Guid? UserId = null,
    Guid? GroupId = null,
    Guid? DepartmentId = null);

public sealed record WorkflowActionDefinition(
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
    IReadOnlyDictionary<string, WorkflowActionValueDefinition>? Values = null);

public sealed record WorkflowActionValueDefinition(
    object? Literal = null,
    string? SourceFieldId = null);

public sealed record WorkflowDefinitionConfig(
    int SchemaVersion,
    string InitialStateKey,
    IReadOnlyList<WorkflowStateDefinition> States,
    IReadOnlyList<WorkflowTransitionDefinition> Transitions,
    IReadOnlyList<WorkflowApprovalStepDefinition> ApprovalSteps);

public sealed record CreateWorkflowDefinitionRequest(
    string Name,
    string? Description,
    WorkflowDefinitionConfig Config,
    bool IsEnabled = true);

public sealed record UpdateWorkflowDefinitionRequest(
    string Name,
    string? Description,
    WorkflowDefinitionConfig Config,
    bool IsEnabled,
    string ConcurrencyStamp);

public sealed record WorkflowStateChangeRequest(string ConcurrencyStamp);

public sealed record WorkflowSummaryDto(
    Guid Id,
    Guid FormId,
    string Name,
    string? Description,
    string Status,
    bool IsEnabled,
    bool HasUnpublishedChanges,
    Guid? CurrentVersionId,
    int? CurrentVersionNumber,
    int StateCount,
    int TransitionCount,
    int ApprovalStepCount,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById);

public sealed record WorkflowDetailDto(
    Guid Id,
    Guid FormId,
    string Name,
    string? Description,
    string Status,
    bool IsEnabled,
    bool HasUnpublishedChanges,
    Guid? CurrentVersionId,
    int? CurrentVersionNumber,
    WorkflowDefinitionConfig Config,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById);

public sealed record WorkflowValidationError(string Path, string Code, string Message);

public sealed record WorkflowValidationResult(IReadOnlyList<WorkflowValidationError> Errors)
{
    public bool Valid => Errors.Count == 0;
}

public sealed record WorkflowErrorResponse(string Message, IReadOnlyList<WorkflowValidationError>? Errors = null);

public sealed class WorkflowManagementException : Exception
{
    public WorkflowManagementException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = Array.Empty<WorkflowValidationError>();
    }

    public WorkflowManagementException(int statusCode, string message, IReadOnlyList<WorkflowValidationError> errors)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public int StatusCode { get; }

    public IReadOnlyList<WorkflowValidationError> Errors { get; }
}
