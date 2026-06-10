namespace OpenBusinessPlatform.Api.Modules.Integrations;

public static class ExternalExportJobSourceTypes
{
    public const string FormRecords = "form_records";
    public const string ListReport = "list_report";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        FormRecords,
        ListReport
    };
}

public static class ExternalExportJobFormats
{
    public const string Csv = "csv";
    public const string Json = "json";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Csv,
        Json
    };
}

public static class ExternalExportJobStatuses
{
    public const string Pending = "pending";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Pending,
        Running,
        Succeeded,
        Failed
    };
}

public sealed record CreateExternalExportJobRequest(
    string SourceType,
    string Format,
    string IntegrationKey,
    Guid? FormId = null,
    Guid? ReportId = null,
    string? Search = null);

public sealed record ExternalExportArtifact(
    string FileName,
    string ContentType,
    string Content,
    long SizeBytes);

public sealed record ExternalExportJobSummaryDto(
    Guid Id,
    string SourceType,
    string Format,
    string IntegrationKey,
    Guid? FormId,
    Guid? ReportId,
    string Status,
    int RowCount,
    string? ArtifactFileName,
    string? ArtifactContentType,
    long ArtifactSizeBytes,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset CreatedAt,
    Guid? CreatedById);

public sealed record ExternalExportJobDetailDto(
    Guid Id,
    string SourceType,
    string Format,
    string IntegrationKey,
    Guid? FormId,
    Guid? ReportId,
    string Status,
    int RowCount,
    string? ArtifactFileName,
    string? ArtifactContentType,
    long ArtifactSizeBytes,
    string? ArtifactContent,
    IReadOnlyDictionary<string, object?>? ArtifactMetadata,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById);

public sealed class ExternalExportException : Exception
{
    public ExternalExportException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}
