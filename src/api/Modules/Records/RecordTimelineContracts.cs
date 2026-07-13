namespace OpenBusinessPlatform.Api.Modules.Records;

public static class RecordTimelineSources
{
    public const string Audit = "audit";
    public const string Workflow = "workflow";
    public const string Trigger = "trigger";
    public const string Integration = "integration";
}

public sealed record RecordTimelineEntryDto(
    string Id,
    string Source,
    string Action,
    string? Status,
    string Summary,
    DateTimeOffset OccurredAt,
    Guid? ActorUserId);

public sealed record RecordTimelineDto(Guid RecordId, IReadOnlyList<RecordTimelineEntryDto> Items);
