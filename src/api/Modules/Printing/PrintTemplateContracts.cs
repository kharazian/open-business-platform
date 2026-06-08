namespace OpenBusinessPlatform.Api.Modules.Printing;

public static class PrintTemplateSectionKinds
{
    public const string Fields = "fields";
    public const string Table = "table";
    public const string Signature = "signature";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Fields,
        Table,
        Signature
    };
}

public sealed record PrintTemplateHeaderConfig(
    string Title,
    string? Subtitle,
    string? LogoUrl,
    bool ShowGeneratedAt);

public static class PrintTemplatePageSizes
{
    public const string Letter = "letter";
    public const string A4 = "a4";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Letter,
        A4
    };
}

public static class PrintTemplateOrientations
{
    public const string Portrait = "portrait";
    public const string Landscape = "landscape";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Portrait,
        Landscape
    };
}

public static class PrintTemplateMargins
{
    public const string Narrow = "narrow";
    public const string Normal = "normal";
    public const string Wide = "wide";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Narrow,
        Normal,
        Wide
    };
}

public sealed record PrintTemplateLayoutConfig(
    string PageSize,
    string Orientation,
    string Margin,
    bool RepeatTableHeaders);

public sealed record PrintTemplateSectionPaginationConfig(
    bool PageBreakBefore,
    bool AvoidBreakInside);

public sealed record PrintTemplateSectionConfig(
    string Id,
    string Kind,
    string Title,
    IReadOnlyList<string> FieldIds,
    IReadOnlyList<string>? SignatureLabels,
    PrintTemplateSectionPaginationConfig? Pagination = null);

public sealed record PrintTemplateFooterConfig(string? Text);

public sealed record PrintTemplateConfig(
    int SchemaVersion,
    string Type,
    PrintTemplateHeaderConfig Header,
    IReadOnlyList<PrintTemplateSectionConfig> Sections,
    PrintTemplateFooterConfig Footer,
    PrintTemplateLayoutConfig? Layout = null);

public sealed record CreatePrintTemplateRequest(
    string Name,
    string? Description,
    string Type,
    Guid? ReportId,
    PrintTemplateConfig Config);

public sealed record UpdatePrintTemplateRequest(
    string Name,
    string? Description,
    string Type,
    Guid? ReportId,
    PrintTemplateConfig Config,
    string ConcurrencyStamp);

public sealed record PrintTemplateSummaryDto(
    Guid Id,
    Guid FormId,
    Guid? ReportId,
    string Name,
    string? Description,
    string Type,
    int SectionCount,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById);

public sealed record PrintTemplateDetailDto(
    Guid Id,
    Guid FormId,
    Guid? ReportId,
    string Name,
    string? Description,
    string Type,
    PrintTemplateConfig Config,
    string ConcurrencyStamp,
    DateTimeOffset CreatedAt,
    Guid? CreatedById,
    DateTimeOffset? UpdatedAt,
    Guid? UpdatedById);

public sealed record PrintTemplateValidationError(string Path, string Code, string Message);

public sealed record PrintTemplateValidationResult(IReadOnlyList<PrintTemplateValidationError> Errors)
{
    public bool Valid => Errors.Count == 0;
}

public sealed record PrintTemplateErrorResponse(string Message, IReadOnlyList<PrintTemplateValidationError>? Errors = null);

public sealed class PrintTemplateException : Exception
{
    public PrintTemplateException(int statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = Array.Empty<PrintTemplateValidationError>();
    }

    public PrintTemplateException(int statusCode, string message, IReadOnlyList<PrintTemplateValidationError> errors)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }

    public int StatusCode { get; }

    public IReadOnlyList<PrintTemplateValidationError> Errors { get; }
}
