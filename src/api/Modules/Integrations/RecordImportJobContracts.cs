using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Integrations;

public static class RecordImportJobStatuses
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string CompletedWithErrors = "completed_with_errors";
    public const string Failed = "failed";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Pending,
        Running,
        Succeeded,
        CompletedWithErrors,
        Failed
    };
}

public static class RecordImportJobRowStatuses
{
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Succeeded,
        Failed
    };
}

public sealed record RecordImportFieldMappingDefinition(
    string CsvHeader,
    string TargetFieldId);

public sealed record RecordImportMappingDefinition(
    IReadOnlyList<RecordImportFieldMappingDefinition> FieldMappings);

public sealed record CreateRecordImportJobRequest(
    Guid FormId,
    string IntegrationKey,
    string? FileName,
    string CsvContent,
    RecordImportMappingDefinition Mapping);

public sealed record RecordImportJobSummaryDto(
    Guid Id,
    Guid FormId,
    string IntegrationKey,
    string? FileName,
    string Status,
    int TotalRows,
    int SucceededRows,
    int FailedRows,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt,
    Guid? CreatedById);

public sealed record RecordImportJobDetailDto(
    Guid Id,
    Guid FormId,
    string IntegrationKey,
    string? FileName,
    string Status,
    int TotalRows,
    int SucceededRows,
    int FailedRows,
    RecordImportMappingDefinition Mapping,
    IReadOnlyList<RecordImportJobRowDto> Rows,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById);

public sealed record RecordImportJobRowDto(
    Guid Id,
    int RowNumber,
    string Status,
    Guid? RecordId,
    IReadOnlyList<FormValidationError> Errors);

public sealed record RecordImportCsvDocument(
    IReadOnlyList<string> Headers,
    IReadOnlyList<RecordImportCsvRow> Rows);

public sealed record RecordImportCsvRow(
    int RowNumber,
    IReadOnlyDictionary<string, string?> Values);

public sealed record RecordImportValidationResult(IReadOnlyList<FormValidationError> Errors)
{
    public bool Valid => Errors.Count == 0;
}

public sealed class RecordImportException : Exception
{
    public RecordImportException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
