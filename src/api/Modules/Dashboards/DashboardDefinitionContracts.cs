using OpenBusinessPlatform.Api.Modules.Dashboard;
using OpenBusinessPlatform.Api.Modules.Forms;

namespace OpenBusinessPlatform.Api.Modules.Dashboards;

public static class DashboardWidgetWidths
{
    public const string Small = "small";
    public const string Medium = "medium";
    public const string Wide = "wide";
    public const string Full = "full";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Small,
        Medium,
        Wide,
        Full
    };
}

public sealed record SavedDashboardWidgetDefinition(
    string Id,
    string Title,
    Guid SourceFormId,
    ChartWidgetConfigDefinition Chart);

public sealed record SavedDashboardConfigDefinition(
    int SchemaVersion,
    IReadOnlyList<SavedDashboardWidgetDefinition> Widgets);

public sealed record SavedDashboardWidgetLayoutDefinition(
    string Id,
    string Width,
    int Order);

public sealed record SavedDashboardLayoutDefinition(
    int SchemaVersion,
    IReadOnlyList<SavedDashboardWidgetLayoutDefinition> Widgets);

public sealed record CreateDashboardRequest(
    string Name,
    string? Description,
    SavedDashboardConfigDefinition Config,
    SavedDashboardLayoutDefinition Layout);

public sealed record UpdateDashboardRequest(
    string Name,
    string? Description,
    SavedDashboardConfigDefinition Config,
    SavedDashboardLayoutDefinition Layout,
    string ConcurrencyStamp);

public sealed record DashboardSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    int WidgetCount,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById);

public sealed record DashboardDetailDto(
    Guid Id,
    string Name,
    string? Description,
    SavedDashboardConfigDefinition Config,
    SavedDashboardLayoutDefinition Layout,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById);

public sealed record DashboardValidationError(string Path, string Code, string Message);

public sealed record DashboardValidationResult(IReadOnlyList<DashboardValidationError> Errors)
{
    public bool Valid => Errors.Count == 0;
}

public sealed record DashboardErrorResponse(string Message, IReadOnlyList<DashboardValidationError>? Errors = null);

public sealed record DashboardSourceReportDefinition(Guid Id, string Type);

public sealed record DashboardSourceDefinition(
    Guid FormId,
    FormSchemaDefinition Schema,
    IReadOnlyList<DashboardSourceReportDefinition> Reports);

public sealed class DashboardDefinitionException : Exception
{
    public DashboardDefinitionException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = Array.Empty<DashboardValidationError>();
    }

    public DashboardDefinitionException(int statusCode, string message, IReadOnlyList<DashboardValidationError> errors)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public int StatusCode { get; }

    public IReadOnlyList<DashboardValidationError> Errors { get; }
}
