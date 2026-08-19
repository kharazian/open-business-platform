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

public sealed record CreateProcessingJobRequest(
    string Name,
    string Kind,
    ProcessingJobConfigDefinition Config,
    ProcessingJobScheduleDefinition? Schedule = null,
    ProcessingJobRetryPolicyDefinition? RetryPolicy = null,
    bool IsEnabled = false)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record UpdateProcessingJobRequest(
    string Name,
    ProcessingJobConfigDefinition Config,
    ProcessingJobScheduleDefinition? Schedule,
    ProcessingJobRetryPolicyDefinition? RetryPolicy,
    string ConcurrencyStamp)
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
