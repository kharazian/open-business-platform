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

public sealed record PrintTemplateSectionConfig(
    string Id,
    string Kind,
    string Title,
    IReadOnlyList<string> FieldIds,
    IReadOnlyList<string>? SignatureLabels);

public sealed record PrintTemplateFooterConfig(string? Text);

public sealed record PrintTemplateConfig(
    int SchemaVersion,
    string Type,
    PrintTemplateHeaderConfig Header,
    IReadOnlyList<PrintTemplateSectionConfig> Sections,
    PrintTemplateFooterConfig Footer);

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
