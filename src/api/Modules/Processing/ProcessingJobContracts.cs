using System.Text.Json;
using System.Text.Json.Serialization;
using OpenBusinessPlatform.Api.Modules.Integrations;

namespace OpenBusinessPlatform.Api.Modules.Processing;

public static class ProcessingJobKinds
{
    public const string CsvRecordImport = "csv_record_import";
    public const string RecordExport = "record_export";
    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal) { CsvRecordImport, RecordExport };
}

public static class ProcessingJobStatuses
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";
    public static IReadOnlySet<string> Active { get; } = new HashSet<string>(StringComparer.Ordinal) { Pending, Running };
}

public static class ProcessingJobRunSources
{
    public const string Manual = "manual";
    public const string Scheduled = "scheduled";
    public const string Retry = "retry";
}

public static class ProcessingOperationalLogSeverities
{
    public const string Info = "info";
    public const string Warning = "warning";
    public const string Error = "error";
    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal) { Info, Warning, Error };
}

public static class ProcessingOperationalEventCodes
{
    public const string RunQueued = "run_queued";
    public const string RunStarted = "run_started";
    public const string RunSucceeded = "run_succeeded";
    public const string RunFailed = "run_failed";
    public const string RetryScheduled = "retry_scheduled";
    public const string RetryExhausted = "retry_exhausted";
    public const string ImportRecoveryUnsafe = "import_recovery_unsafe";
    public const string ScheduleSkippedActiveRun = "schedule_skipped_active_run";
    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        RunQueued, RunStarted, RunSucceeded, RunFailed, RetryScheduled, RetryExhausted,
        ImportRecoveryUnsafe, ScheduleSkippedActiveRun
    };
}

public static class ProcessingScheduleKinds
{
    public const string Once = "once";
    public const string Daily = "daily";
    public const string Weekly = "weekly";
    public const string Monthly = "monthly";
    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal) { Once, Daily, Weekly, Monthly };
}

public sealed record ProcessingJobConfigDefinition(
    Guid FormId,
    string IntegrationKey,
    string? SourceType = null,
    string? Format = null,
    Guid? ReportId = null,
    string? Search = null,
    int MaxRows = 1000,
    RecordImportMappingDefinition? Mapping = null)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ProcessingJobScheduleDefinition(
    string Kind,
    string TimeZone,
    DateTimeOffset StartAt,
    int Interval = 1,
    int? DayOfWeek = null,
    int? DayOfMonth = null)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ProcessingJobRetryPolicyDefinition(bool IsEnabled = false, int MaxAttempts = 1, int DelaySeconds = 300)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ProcessingFailureNotificationPolicyDefinition(
    bool IsEnabled = false,
    bool IncludeOwner = false,
    IReadOnlyList<Guid>? RecipientUserIds = null)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record CreateProcessingJobRequest(
    string Name,
    string Kind,
    ProcessingJobConfigDefinition Config,
    ProcessingJobScheduleDefinition? Schedule = null,
    ProcessingJobRetryPolicyDefinition? RetryPolicy = null,
    bool IsEnabled = false,
    ProcessingFailureNotificationPolicyDefinition? FailureNotificationPolicy = null)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record UpdateProcessingJobRequest(
    string Name,
    ProcessingJobConfigDefinition Config,
    ProcessingJobScheduleDefinition? Schedule,
    ProcessingJobRetryPolicyDefinition? RetryPolicy,
    string ConcurrencyStamp,
    ProcessingFailureNotificationPolicyDefinition? FailureNotificationPolicy = null)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ProcessingJobStateRequest(string ConcurrencyStamp);
public sealed record CreateProcessingJobRunRequest(string? FileName = null, string? CsvContent = null)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ProcessingJobSummaryDto(
    Guid Id,
    string Name,
    string Kind,
    bool IsEnabled,
    Guid FormId,
    Guid? ReportId,
    DateTimeOffset? NextRunAt,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record ProcessingJobDetailDto(
    Guid Id,
    string Name,
    string Kind,
    ProcessingJobConfigDefinition Config,
    ProcessingJobScheduleDefinition? Schedule,
    ProcessingJobRetryPolicyDefinition RetryPolicy,
    ProcessingFailureNotificationPolicyDefinition FailureNotificationPolicy,
    bool IsEnabled,
    Guid OwnerUserId,
    DateTimeOffset? NextRunAt,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById);

public sealed record ProcessingJobRunDto(
    Guid Id,
    Guid DefinitionId,
    string Source,
    string Status,
    int Attempt,
    int MaxAttempts,
    DateTimeOffset NextAttemptAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    string? ErrorCode,
    string? ErrorMessage,
    string? InputFileName,
    long InputSizeBytes,
    string? InputChecksum,
    Guid? RecordImportJobId,
    Guid? ExternalExportJobId,
    Guid? RetrySourceRunId,
    DateTimeOffset CreatedAt,
    Guid? CreatedById);

public sealed record ProcessingJobPageDto<T>(IReadOnlyList<T> Items, int Page, int PageSize, long TotalCount);
public sealed record ProcessingOperationalLogDto(
    Guid Id,
    Guid DefinitionId,
    string DefinitionName,
    Guid? RunId,
    string Kind,
    string Severity,
    string EventCode,
    string Message,
    int? Attempt,
    int? MaxAttempts,
    string? ErrorCode,
    long? DurationMilliseconds,
    Guid? RecordImportJobId,
    Guid? ExternalExportJobId,
    DateTimeOffset OccurredAt);
public sealed record ProcessingOperationsSummaryDto(
    DateTimeOffset From,
    DateTimeOffset To,
    long Pending,
    long Running,
    long Succeeded,
    long Failed,
    long RetryScheduled,
    long RetryExhausted,
    long ScheduleSkipped,
    IReadOnlyDictionary<string, long> ByKind);
public sealed record ProcessingNotificationRecipientDto(Guid Id, string Name);
public sealed record ProcessingJobValidationError(string Path, string Code, string Message);
public sealed record ProcessingJobValidationResult(IReadOnlyList<ProcessingJobValidationError> Errors) { public bool Valid => Errors.Count == 0; }
public sealed record ProcessingJobErrorResponse(string Message, IReadOnlyList<ProcessingJobValidationError>? Errors = null);

public sealed class ProcessingJobException : Exception
{
    public ProcessingJobException(int statusCode, string message, IReadOnlyList<ProcessingJobValidationError>? errors = null) : base(message)
    {
        StatusCode = statusCode;
        Errors = errors ?? Array.Empty<ProcessingJobValidationError>();
    }
    public int StatusCode { get; }
    public IReadOnlyList<ProcessingJobValidationError> Errors { get; }
}
