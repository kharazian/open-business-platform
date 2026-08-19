using System.Text.Json;
using System.Text.Json.Serialization;
using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Reports;

public static class ReportFilterOperators
{
    public const string Equal = "equals";
    public const string Contains = "contains";
    public const string GreaterThan = "greater_than";
    public const string GreaterOrEqual = "greater_or_equal";
    public const string LessThan = "less_than";
    public const string LessOrEqual = "less_or_equal";
    public const string Before = "before";
    public const string After = "after";
    public const string IsEmpty = "is_empty";
    public const string IsNotEmpty = "is_not_empty";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Equal,
        Contains,
        GreaterThan,
        GreaterOrEqual,
        LessThan,
        LessOrEqual,
        Before,
        After,
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

public static class ListReportRowOpenActions
{
    public const string Detail = "detail";
    public const string Edit = "edit";
    public const string None = "none";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Detail,
        Edit,
        None
    };
}

public static class ListReportActionTypes
{
    public const string CreateRecord = "create_record";
    public const string PrintReport = "print_report";
    public const string ExportCsv = "export_csv";
    public const string ViewRecord = "view_record";
    public const string EditRecord = "edit_record";
    public const string DeleteRecord = "delete_record";

    public static IReadOnlySet<string> Report { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        CreateRecord,
        PrintReport,
        ExportCsv
    };

    public static IReadOnlySet<string> Row { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        ViewRecord,
        EditRecord,
        DeleteRecord
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

public sealed record ListReportActionDefinition(
    string Id,
    string Type,
    string Label,
    bool Enabled = true,
    string? Confirmation = null)
{
    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; init; }
}

public sealed record ListReportConfigDefinition(
    int SchemaVersion,
    IReadOnlyList<ListReportColumnDefinition> Columns,
    IReadOnlyList<ListReportFilterDefinition> Filters,
    IReadOnlyList<ListReportSortDefinition> Sort,
    string? RowOpenAction = null)
{
    public IReadOnlyList<ListReportActionDefinition>? ReportActions { get; init; }

    public IReadOnlyList<ListReportActionDefinition>? RowActions { get; init; }
}

public sealed record CreateListReportRequest(string Name, ListReportConfigDefinition Config);

public sealed record UpdateListReportRequest(string Name, ListReportConfigDefinition Config, string ConcurrencyStamp);

public sealed record RunListReportRequest(
    int Page = 1,
    int PageSize = 25,
    string? Search = null,
    string? SortFieldId = null,
    string? SortDirection = null,
    IReadOnlyDictionary<string, string?>? Filters = null);

public sealed record ListReportExecutionColumnDto(
    string FieldId,
    string Label,
    string Type,
    string Source,
    int? Width);

public sealed record ListReportExecutionCellDto(object? Value, string DisplayValue);

public sealed record ResolvedReportFieldValue(object? Value, string DisplayValue);

public sealed record ReportFieldCatalogDto(IReadOnlyList<ReportableFieldMetadata> Items);

public sealed record ListReportResolvedActionDto(string Id, string Type, string Label, string? Confirmation = null);

public sealed record ListReportExecutionRowDto(
    Guid RecordId,
    string Status,
    IReadOnlyDictionary<string, ListReportExecutionCellDto> Cells,
    DateTimeOffset CreatedAt)
{
    public IReadOnlyList<ListReportResolvedActionDto> Actions { get; init; } = Array.Empty<ListReportResolvedActionDto>();
}

public sealed record ListReportExecutionDto(
    Guid ReportId,
    Guid FormId,
    string ReportName,
    string FormName,
    int Page,
    int PageSize,
    long TotalCount,
    IReadOnlyList<ListReportExecutionColumnDto> Columns,
    IReadOnlyList<ListReportExecutionRowDto> Rows)
{
    public IReadOnlyList<ListReportResolvedActionDto> ReportActions { get; init; } = Array.Empty<ListReportResolvedActionDto>();
}

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

public sealed record ListReportCsvExportDto(string FileName, string Content);

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
