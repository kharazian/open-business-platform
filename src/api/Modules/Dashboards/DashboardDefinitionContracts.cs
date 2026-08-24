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
    Guid? SourceFormId,
    ChartWidgetConfigDefinition? Chart,
    string? SectionId = null,
    DashboardAdapterWidgetDefinition? Adapter = null,
    string? Subtitle = null,
    DashboardWidgetInteractionDefinition? Interaction = null);

public sealed record DashboardWidgetInteractionDefinition(
    string Destination,
    Guid? ReportId = null,
    bool IncludeDashboardFilters = true,
    bool IncludePointFilter = true,
    bool OpenInNewTab = false);

public sealed record DashboardAdapterWidgetDefinition(
    string AdapterId,
    string VisualizationId,
    IReadOnlyDictionary<string, object?> Settings);

public sealed record SavedDashboardSectionDefinition(string Id, string Title, int Order, string? Icon = null);

public sealed record DashboardTemplateProvenanceDefinition(
    string TemplateId,
    int TemplateVersion,
    DateTimeOffset InstantiatedAt);

public sealed record SavedDashboardFilterValueDefinition(string FieldId, IReadOnlyList<string>? Values = null, string? Start = null, string? End = null);

public sealed record SavedDashboardFilterDefinition(
    string Id,
    string Label,
    string Type,
    Guid SourceFormId,
    string FieldId,
    IReadOnlyList<string>? Options = null,
    IReadOnlyList<string>? ApplyToWidgetIds = null,
    SavedDashboardFilterValueDefinition? DefaultValue = null,
    bool Required = false);

public sealed record SavedDashboardConfigDefinition(
    int SchemaVersion,
    IReadOnlyList<SavedDashboardWidgetDefinition> Widgets,
    IReadOnlyList<SavedDashboardSectionDefinition>? Sections = null,
    DashboardTemplateProvenanceDefinition? TemplateProvenance = null,
    IReadOnlyList<SavedDashboardFilterDefinition>? Filters = null);

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
    SavedDashboardLayoutDefinition Layout,
    DashboardSettingsDefinition? Settings = null,
    DashboardPublicationSettingsDefinition? Publication = null);

public sealed record UpdateDashboardRequest(
    string Name,
    string? Description,
    SavedDashboardConfigDefinition Config,
    SavedDashboardLayoutDefinition Layout,
    string ConcurrencyStamp,
    DashboardSettingsDefinition? Settings = null,
    DashboardPublicationSettingsDefinition? Publication = null);

public sealed record DashboardPublicationSettingsDefinition(
    string Status,
    string? Slug,
    bool ShowInNavigation,
    string? MenuLabel,
    string? MenuIcon,
    int MenuOrder,
    string? ViewPermission);

public sealed record DashboardPublicationMutationRequest(string ConcurrencyStamp);
public sealed record DashboardRevisionRestoreRequest(string ConcurrencyStamp);

public sealed record DashboardRevisionSnapshotDefinition(
    string Name,
    string? Description,
    SavedDashboardConfigDefinition Config,
    SavedDashboardLayoutDefinition Layout,
    DashboardSettingsDefinition Settings,
    DashboardPublicationSettingsDefinition Publication);

public sealed record DashboardRevisionSummaryDto(Guid Id, int RevisionNumber, string Reason, DateTimeOffset CreatedAt, Guid? CreatedById, bool IsPublished);

public sealed record DashboardPublishedComparisonDto(bool HasPublishedVersion, DashboardRevisionSnapshotDefinition? Published, DateTimeOffset? PublishedAt, Guid? PublishedById);

public sealed record DashboardSharingSettingsDto(IReadOnlyCollection<Guid> UserIds, IReadOnlyCollection<Guid> RoleIds, IReadOnlyCollection<Guid> GroupIds);
public sealed record DashboardSharingOptionDto(Guid Id, string Label, string? Description = null);
public sealed record DashboardSharingOptionsDto(
    IReadOnlyCollection<DashboardSharingOptionDto> Users,
    IReadOnlyCollection<DashboardSharingOptionDto> Roles,
    IReadOnlyCollection<DashboardSharingOptionDto> Groups);

public sealed record DashboardSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    int WidgetCount,
    string Visibility,
    bool IsDefault,
    DashboardPublicationSettingsDefinition Publication,
    DateTimeOffset? PublishedAt,
    Guid? PublishedById,
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
    string Visibility,
    bool IsDefault,
    DashboardPublicationSettingsDefinition Publication,
    DateTimeOffset? PublishedAt,
    Guid? PublishedById,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById);

public sealed record DashboardNavigationItemDto(Guid Id, string Slug, string Label, string? Icon, int Order);

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
