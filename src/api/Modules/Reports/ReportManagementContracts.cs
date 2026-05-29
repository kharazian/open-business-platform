using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Reports;

public static class ReportFilterOperators
{
    public const string Equal = "equals";
    public const string Contains = "contains";
    public const string IsEmpty = "is_empty";
    public const string IsNotEmpty = "is_not_empty";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Equal,
        Contains,
        IsEmpty,
        IsNotEmpty
    };
}

public static class ReportSortDirections
{
    public const string Asc = "asc";
    public const string Desc = "desc";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Asc,
        Desc
    };
}

public static class ReportSystemFields
{
    public const string Status = ReportableSystemFields.Status;
    public const string CreatedAt = ReportableSystemFields.CreatedAt;
    public const string CreatedById = ReportableSystemFields.CreatedById;
    public const string UpdatedAt = ReportableSystemFields.UpdatedAt;
    public const string UpdatedById = ReportableSystemFields.UpdatedById;
    public const string OwnerId = ReportableSystemFields.OwnerId;
    public const string DepartmentId = ReportableSystemFields.DepartmentId;

    public static IReadOnlySet<string> Supported { get; } = FormReportableFieldMetadata.SystemFields
        .Select(field => field.Id)
        .ToHashSet(StringComparer.Ordinal);
}

public sealed record ListReportColumnDefinition(string FieldId, string Label, bool Visible = true, int? Width = null);

public sealed record ListReportFilterDefinition(string FieldId, string Operator, string? Value = null);

public sealed record ListReportSortDefinition(string FieldId, string Direction);

public sealed record ListReportConfigDefinition(
    int SchemaVersion,
    IReadOnlyList<ListReportColumnDefinition> Columns,
    IReadOnlyList<ListReportFilterDefinition> Filters,
    IReadOnlyList<ListReportSortDefinition> Sort);

public sealed record CreateListReportRequest(string Name, ListReportConfigDefinition Config);

public sealed record ListReportSummaryDto(
    Guid Id,
    Guid FormId,
    string FormName,
    string Name,
    string Type,
    int ColumnCount,
    int FilterCount,
    int SortCount,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById);

public sealed record ListReportDetailDto(
    Guid Id,
    Guid FormId,
    string FormName,
    string Name,
    string Type,
    ListReportConfigDefinition Config,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById);

public sealed record ReportValidationError(string Path, string Code, string Message);

public sealed record ReportValidationResult(IReadOnlyList<ReportValidationError> Errors)
{
    public bool Valid => Errors.Count == 0;
}

public sealed record ReportErrorResponse(string Message, IReadOnlyList<ReportValidationError>? Errors = null);

public sealed class ReportManagementException : Exception
{
    public ReportManagementException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = Array.Empty<ReportValidationError>();
    }

    public ReportManagementException(int statusCode, string message, IReadOnlyList<ReportValidationError> errors)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public int StatusCode { get; }

    public IReadOnlyList<ReportValidationError> Errors { get; }
}
