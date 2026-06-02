namespace OpenBusinessPlatform.Api.Modules.Triggers;

public sealed record TriggerRecordSnapshot(
    Guid RecordId,
    Guid FormId,
    string Status,
    Guid? OwnerId,
    Guid? DepartmentId,
    Guid? AssignedToUserId,
    Guid? AssignedGroupId,
    IReadOnlyDictionary<string, object?> Values);

public sealed record TriggerEventContext(
    string EventName,
    Guid FormId,
    Guid RecordId,
    Guid? ActorUserId,
    TriggerRecordSnapshot? Before,
    TriggerRecordSnapshot After,
    IReadOnlyCollection<string> ChangedFieldIds,
    string? PreviousStatus,
    string? CurrentStatus,
    Guid? PreviousAssignedToUserId,
    Guid? CurrentAssignedToUserId,
    Guid? PreviousAssignedGroupId,
    Guid? CurrentAssignedGroupId,
    DateTimeOffset OccurredAt);
